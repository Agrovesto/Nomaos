using System.Net;
using System.Text;
using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;

namespace NomaOS.Test;

public class CafeOneBankCacheTests
{
    private const string Env = "Demo";
    private const long ProviderId = 7;
    private const string PayoutUrl = "https://payout.test";

    private static readonly string[] ParameterKeys =
    {
        "url", "secretkey", "publickey", "organizationId", "ownerId", "email", "password",
        "accountNumber", "customerTypeId", "countryId", "currencyId", "currency",
        "debitdirection", "creditdirection", "productCredit", "productDebit", "payoutUrl"
    };

    private static IDistributedCache CreateCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static Mock<IWalletProviderRepository> CreateProviderRepo()
    {
        var repo = new Mock<IWalletProviderRepository>();

        repo.Setup(x => x.GetActiveProvider())
            .ReturnsAsync(new[] { new WalletProviderModel { ProviderId = ProviderId, Code = "CafeOne" } });

        repo.Setup(x => x.GetParametersByProvider(ProviderId, Env))
            .ReturnsAsync(ParameterKeys
                .Select(k => new WalletProviderParameterModel
                {
                    ProviderId = ProviderId,
                    Key = k,
                    Value = k == "payoutUrl" ? PayoutUrl : $"{k}-value"
                })
                .ToArray());

        return repo;
    }

    private static HttpResponseMessage BanksResponse(params string[] bankNames)
    {
        var payload = JsonConvert.SerializeObject(new
        {
            data = bankNames.Select(n => new { bankName = n, bankCode = n.Length.ToString() }).ToArray()
        });

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
    }

    private static CafeOneWalletService CreateService(IHttpService httpService, IDistributedCache cache) =>
        new(
            Mock.Of<ILogger<CafeOneWalletService>>(),
            httpService,
            CreateProviderRepo().Object,
            Mock.Of<IWalletRepository>(),
            cache);

    private static Mock<IHttpService> CreateHttpService(Func<HttpResponseMessage> banks)
    {
        var httpService = new Mock<IHttpService>();

        httpService
            .Setup(x => x.Post(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"accessToken\":\"t\"}}", Encoding.UTF8, "application/json")
            });

        httpService
            .Setup(x => x.Get(It.Is<string>(u => u.Contains("/api/Payout/banks")), It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()))
            .ReturnsAsync(banks);

        return httpService;
    }

    [Fact]
    public async Task GetBanks_OnTheFirstCall_HitsTheProviderAndCachesTheResult()
    {
        var cache = CreateCache();
        var httpService = CreateHttpService(() => BanksResponse("Access", "GTB"));

        var service = CreateService(httpService.Object, cache);

        var banks = await service.GetBanks(Env);

        banks.Should().HaveCount(2);

        httpService.Verify(x => x.Get(
            It.Is<string>(u => u.Contains("/api/Payout/banks")), It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()),
            Times.Once);

        (await cache.GetStringAsync($"cafeone:banks:demo")).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetBanks_OnASecondCall_ServesFromCacheWithoutCallingTheProvider()
    {
        var cache = CreateCache();
        var httpService = CreateHttpService(() => BanksResponse("Access", "GTB"));

        var service = CreateService(httpService.Object, cache);

        var first = await service.GetBanks(Env);
        var second = await service.GetBanks(Env);

        first.Should().HaveCount(2);
        second.Should().HaveCount(2);

        httpService.Verify(x => x.Get(
            It.Is<string>(u => u.Contains("/api/Payout/banks")), It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task GetBanks_WhenTheCacheHasExpired_GoesBackToTheProvider()
    {
        var cache = CreateCache();
        var httpService = CreateHttpService(() => BanksResponse("Access", "GTB"));

        var service = CreateService(httpService.Object, cache);

        await service.GetBanks(Env);

        await cache.RemoveAsync("cafeone:banks:demo");

        await service.GetBanks(Env);

        httpService.Verify(x => x.Get(
            It.Is<string>(u => u.Contains("/api/Payout/banks")), It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetBanks_CachesPerEnvironment()
    {
        var cache = CreateCache();
        var httpService = CreateHttpService(() => BanksResponse("Access"));

        var service = CreateService(httpService.Object, cache);

        await service.GetBanks(Env);

        (await cache.GetStringAsync("cafeone:banks:demo")).Should().NotBeNullOrWhiteSpace();
        (await cache.GetStringAsync("cafeone:banks:live")).Should().BeNull();
    }

    [Fact]
    public async Task GetBanks_WhenTheProviderReturnsNothing_DoesNotPoisonTheCache()
    {
        var cache = CreateCache();

        var httpService = CreateHttpService(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":[]}", Encoding.UTF8, "application/json")
        });

        var service = CreateService(httpService.Object, cache);

        var banks = await service.GetBanks(Env);

        banks.Should().BeEmpty();
        (await cache.GetStringAsync("cafeone:banks:demo")).Should().BeNull();
    }

    [Fact]
    public async Task GetBanks_WhenTheCacheIsUnavailable_StillServesFromTheProvider()
    {
        var cache = new Mock<IDistributedCache>();

        cache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis is down"));
        cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis is down"));

        var httpService = CreateHttpService(() => BanksResponse("Access", "GTB"));

        var service = CreateService(httpService.Object, cache.Object);

        var banks = await service.GetBanks(Env);

        banks.Should().HaveCount(2);
    }
}
