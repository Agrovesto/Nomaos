using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace NomaOS.Test;

public class AgentDisbursementTests
{
    private const string AgentId = "agent-1";
    private const string FarmerOneId = "farmer-1";
    private const string FarmerTwoId = "farmer-2";
    private const string FarmerThreeId = "farmer-3";
    private const string BusinessCode = "BIZ1";
    private const long ProviderId = 3;
    private const string AgentWalletNumber = "2349012981234";
    private const string Pin = "1234";

    private sealed class StubTransferService : IGenericWalletService
    {
        private readonly Func<TransferRequest, WalletResponseService<PaymentResponse>> _transfer;

        public StubTransferService(Func<TransferRequest, WalletResponseService<PaymentResponse>>? transfer = null)
        {
            _transfer = transfer ?? (r => WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Successful", Reference = r.Reference, ProviderReference = "PRV-1" },
                "Transfer completed"));
        }

        public List<TransferRequest> Requests { get; } = new();

        public string ProviderKey => "Momo";
        public bool RequiresPreDebit => true;

        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTransferAsync(
            TransferRequest request, WalletProviderParameterModel[] parameters, string env)
        {
            Requests.Add(request);
            return Task.FromResult(_transfer(request));
        }

        public Task<WalletResponseService<PaymentResponse>> DebitWalletAsync(
            WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string env)
            => Task.FromResult(WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Successful", Reference = request.TransactionReference }, "Debit completed"));

        public Task<ProviderParams> RetrieveParams(WalletProviderParameterModel[] parameters, string env)
            => Task.FromResult(new ProviderParams());

        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTopUpAsync(
            WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string env)
            => throw new NotSupportedException();

        public Task<WalletResponseService<AccountValidationResponse>> CreateWalletAsync(
            UserProfileResponse data, WalletProviderParameterModel[] parameters, string env)
            => throw new NotSupportedException();

        public Task<WalletResponseService<AccountValidationResponse>> BalanceEnquiryAsync(
            UserProfileResponse data, WalletProviderParameterModel[] parameters, string env)
            => throw new NotSupportedException();

        public Task<WalletResponseService<PaymentStatusResponse>> TransactionStatusAsync(
            string reference, WalletProviderParameterModel[] parameters, string env)
            => throw new NotSupportedException();
    }

    private static DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new DataContext(options);
    }

    private static async Task<UserWallet> SeedAgentWalletAsync(DataContext context, decimal balance)
    {
        context.WalletProviders.Add(new WalletProvider
        {
            ProviderId = ProviderId,
            Code = "Momo",
            Name = "MTN Momo",
            Status = "Active"
        });

        var wallet = await SeedWalletAsync(context, AgentId, AgentWalletNumber, balance);

        context.WalletSecurities.Add(new WalletSecurity
        {
            UserWalletId = wallet.Id,
            Txn = Pin,
            TransactionPinHash = BCrypt.Net.BCrypt.HashPassword(Pin)
        });
        await context.SaveChangesAsync();

        return wallet;
    }

    private static async Task<UserWallet> SeedWalletAsync(
        DataContext context,
        string userId,
        string walletNumber,
        decimal balance = 0m,
        bool isActive = true)
    {
        var wallet = new UserWallet
        {
            UserId = userId,
            WalletProviderId = ProviderId,
            WalletBalance = balance,
            Currency = "NGN",
            IsActive = isActive,
            WalletNumber = walletNumber,
            BelongToBusinessCode = BusinessCode,
            IsDeleted = false,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        context.UserWallets.Add(wallet);
        await context.SaveChangesAsync();

        return wallet;
    }

    private static NomaOSWalletService CreateService(DataContext context, IGenericWalletService provider)
    {
        var providerRepo = new Mock<IWalletProviderRepository>();

        providerRepo
            .Setup(x => x.GetById(ProviderId))
            .ReturnsAsync(new WalletProviderModel { ProviderId = ProviderId, Code = "Momo", Name = "MTN Momo" });

        providerRepo
            .Setup(x => x.GetProvidersForBusiness(BusinessCode))
            .ReturnsAsync(new[]
            {
                new BusinessWalletProviderModel
                {
                    BusinessCode = BusinessCode,
                    ProviderId = ProviderId,
                    WalletProvider = new WalletProviderModel { ProviderId = ProviderId, Code = "Momo" }
                }
            });

        providerRepo
            .Setup(x => x.GetParametersByProvider(ProviderId, BusinessCode, It.IsAny<string>()))
            .ReturnsAsync(new[]
            {
                new WalletProviderParameterModel { Key = "BaseUrl", Value = "https://example.test", ProviderId = ProviderId }
            });

        var factory = new Mock<IWalletServiceFactory>();
        factory.Setup(x => x.GetService("Momo")).Returns(provider);

        var onboardingRepository = new Mock<IOnboardingRepository>();
        onboardingRepository
            .Setup(x => x.GetUserById(AgentId))
            .ReturnsAsync((new UserModel { Id = AgentId, Name = "Ada Lovelace" }, true, string.Empty));

        foreach (var farmerId in new[] { FarmerOneId, FarmerTwoId, FarmerThreeId })
        {
            var id = farmerId;
            onboardingRepository
                .Setup(x => x.GetUserById(id))
                .ReturnsAsync((new UserModel { Id = id, Name = $"Farmer {id}" }, true, string.Empty));
        }

        return new NomaOSWalletService(
            Mock.Of<IBusinessRepository>(),
            onboardingRepository.Object,
            new UserWalletRepository(context, Mock.Of<IAWSService>()),
            providerRepo.Object,
            factory.Object,
            context,
            Mock.Of<IMTNMomo>(),
            Mock.Of<IInputFinancingRepository>(),
            Mock.Of<ILogger<NomaOSWalletService>>(),
            new OtpService(context),
            Mock.Of<IAWSService>(),
            Mock.Of<INotificationService>(),
            Mock.Of<IConfiguration>());
    }

    private static WalletQueryParams Query() => new()
    {
        UserId = AgentId,
        businessCode = BusinessCode,
        env = "prod"
    };

    private static WalletDisbursementRequest DisbursementOf(decimal amount, params string[] recipients) => new()
    {
        RecipientUserIds = recipients.ToList(),
        Amount = amount,
        TransactionPin = Pin
    };

    [Fact]
    public async Task DisburseToSubscribers_PaysEachRecipientTheSameAmountFromTheAgentsActiveWallet()
    {
        await using var context = CreateContext();
        var agentWallet = await SeedAgentWalletAsync(context, 10000m);
        var farmerOne = await SeedWalletAsync(context, FarmerOneId, "2349000000001");
        var farmerTwo = await SeedWalletAsync(context, FarmerTwoId, "2349000000002");

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, message, data) = await service.DisburseToSubscribers(
            DisbursementOf(1500m, FarmerOneId, FarmerTwoId), Query());

        status.Should().BeTrue();
        message.Should().Contain("2 recipient(s)");

        data.Should().NotBeNull();
        data!.SourceWalletId.Should().Be(agentWallet.Id);
        data.SourceWalletNumber.Should().Be(AgentWalletNumber);
        data.AmountPerRecipient.Should().Be(1500m);
        data.TotalDisbursed.Should().Be(3000m);
        data.SuccessfulCount.Should().Be(2);
        data.FailedCount.Should().Be(0);
        data.Recipients.Should().OnlyContain(x => x.Succeeded && x.Amount == 1500m);

        provider.Requests.Should().HaveCount(2);
        provider.Requests.Select(x => x.AccountNumber)
            .Should().BeEquivalentTo("2349000000001", "2349000000002");

        var balances = await context.UserWallets.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.WalletBalance);
        balances[agentWallet.Id].Should().Be(7000m);
        balances[farmerOne.Id].Should().Be(1500m);
        balances[farmerTwo.Id].Should().Be(1500m);
    }

    [Fact]
    public async Task DisburseToSubscribers_WithASingleRecipient_PaysThatOneFarmer()
    {
        await using var context = CreateContext();
        var agentWallet = await SeedAgentWalletAsync(context, 5000m);
        var farmer = await SeedWalletAsync(context, FarmerOneId, "2349000000001");

        var service = CreateService(context, new StubTransferService());

        var (status, _, data) = await service.DisburseToSubscribers(
            DisbursementOf(2000m, FarmerOneId), Query());

        status.Should().BeTrue();
        data!.SuccessfulCount.Should().Be(1);
        data.TotalDisbursed.Should().Be(2000m);

        var balances = await context.UserWallets.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.WalletBalance);
        balances[agentWallet.Id].Should().Be(3000m);
        balances[farmer.Id].Should().Be(2000m);
    }

    [Fact]
    public async Task DisburseToSubscribers_WhenTheBatchExceedsTheBalance_SendsNothing()
    {
        await using var context = CreateContext();
        var agentWallet = await SeedAgentWalletAsync(context, 2500m);
        await SeedWalletAsync(context, FarmerOneId, "2349000000001");
        await SeedWalletAsync(context, FarmerTwoId, "2349000000002");

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, message, data) = await service.DisburseToSubscribers(
            DisbursementOf(1500m, FarmerOneId, FarmerTwoId), Query());

        status.Should().BeFalse();
        message.Should().StartWith("Insufficient balance");
        data.Should().BeNull();

        provider.Requests.Should().BeEmpty();

        var balance = await context.UserWallets.AsNoTracking().Where(x => x.Id == agentWallet.Id).Select(x => x.WalletBalance).SingleAsync();
        balance.Should().Be(2500m);
    }

    [Fact]
    public async Task DisburseToSubscribers_WithARecipientWhoHasNoActiveWallet_PaysTheRest()
    {
        await using var context = CreateContext();
        var agentWallet = await SeedAgentWalletAsync(context, 9000m);
        var farmerOne = await SeedWalletAsync(context, FarmerOneId, "2349000000001");
        await SeedWalletAsync(context, FarmerTwoId, "2349000000002", isActive: false);
        var farmerThree = await SeedWalletAsync(context, FarmerThreeId, "2349000000003");

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, message, data) = await service.DisburseToSubscribers(
            DisbursementOf(1000m, FarmerOneId, FarmerTwoId, FarmerThreeId), Query());

        status.Should().BeFalse();
        message.Should().Be("2 of 3 disbursements were successful");

        data!.SuccessfulCount.Should().Be(2);
        data.FailedCount.Should().Be(1);
        data.TotalDisbursed.Should().Be(2000m);

        var skipped = data.Recipients.Single(x => x.RecipientUserId == FarmerTwoId);
        skipped.Succeeded.Should().BeFalse();
        skipped.Message.Should().Be("No active wallet was found for this recipient");
        skipped.TransactionReference.Should().BeNull();

        provider.Requests.Should().HaveCount(2);

        var balances = await context.UserWallets.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.WalletBalance);
        balances[agentWallet.Id].Should().Be(7000m);
        balances[farmerOne.Id].Should().Be(1000m);
        balances[farmerThree.Id].Should().Be(1000m);
    }

    [Fact]
    public async Task DisburseToSubscribers_WithTheWrongPin_SendsNothing()
    {
        await using var context = CreateContext();
        var agentWallet = await SeedAgentWalletAsync(context, 9000m);
        await SeedWalletAsync(context, FarmerOneId, "2349000000001");

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var request = DisbursementOf(1000m, FarmerOneId);
        request.TransactionPin = "9999";

        var (status, _, data) = await service.DisburseToSubscribers(request, Query());

        status.Should().BeFalse();
        data.Should().BeNull();
        provider.Requests.Should().BeEmpty();

        var balance = await context.UserWallets.AsNoTracking().Where(x => x.Id == agentWallet.Id).Select(x => x.WalletBalance).SingleAsync();
        balance.Should().Be(9000m);
    }

    [Fact]
    public async Task DisburseToSubscribers_WithTheSameRecipientTwice_PaysThemOnce()
    {
        await using var context = CreateContext();
        var agentWallet = await SeedAgentWalletAsync(context, 9000m);
        var farmer = await SeedWalletAsync(context, FarmerOneId, "2349000000001");

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, _, data) = await service.DisburseToSubscribers(
            DisbursementOf(1000m, FarmerOneId, FarmerOneId), Query());

        status.Should().BeTrue();
        data!.SuccessfulCount.Should().Be(1);
        provider.Requests.Should().ContainSingle();

        var balances = await context.UserWallets.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.WalletBalance);
        balances[agentWallet.Id].Should().Be(8000m);
        balances[farmer.Id].Should().Be(1000m);
    }

    [Fact]
    public async Task DisburseToSubscribers_WhenTheProviderRejectsOneLeg_ReportsItAndKeepsTheOthers()
    {
        await using var context = CreateContext();
        var agentWallet = await SeedAgentWalletAsync(context, 9000m);
        var farmerOne = await SeedWalletAsync(context, FarmerOneId, "2349000000001");
        var farmerTwo = await SeedWalletAsync(context, FarmerTwoId, "2349000000002");

        var provider = new StubTransferService(r => r.AccountNumber == "2349000000002"
            ? WalletResponseService<PaymentResponse>.Failed("Recipient account is barred")
            : WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Successful", Reference = r.Reference }, "Transfer completed"));

        var service = CreateService(context, provider);

        var (status, message, data) = await service.DisburseToSubscribers(
            DisbursementOf(1000m, FarmerOneId, FarmerTwoId), Query());

        status.Should().BeFalse();
        message.Should().Be("1 of 2 disbursements were successful");

        data!.Recipients.Single(x => x.RecipientUserId == FarmerTwoId).Message.Should().Be("Recipient account is barred");

        var balances = await context.UserWallets.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.WalletBalance);
        balances[agentWallet.Id].Should().Be(8000m);
        balances[farmerOne.Id].Should().Be(1000m);
        balances[farmerTwo.Id].Should().Be(0m);
    }

    [Fact]
    public async Task DisburseToSubscribers_WithNoRecipients_IsRejected()
    {
        await using var context = CreateContext();
        await SeedAgentWalletAsync(context, 9000m);

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, message, data) = await service.DisburseToSubscribers(DisbursementOf(1000m), Query());

        status.Should().BeFalse();
        message.Should().Be("At least one recipient is required");
        data.Should().BeNull();
        provider.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task DisburseToSubscribers_ForAnAgentWithNoActiveWallet_IsRejected()
    {
        await using var context = CreateContext();
        await SeedWalletAsync(context, FarmerOneId, "2349000000001");

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, message, data) = await service.DisburseToSubscribers(
            DisbursementOf(1000m, FarmerOneId), Query());

        status.Should().BeFalse();
        message.Should().Be("No active wallet was found for this agent");
        data.Should().BeNull();
        provider.Requests.Should().BeEmpty();
    }
}
