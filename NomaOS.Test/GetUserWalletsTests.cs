using Agrovesto.Backend.API.Controllers.V2.Internal;
using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace NomaOS.Test;

public class GetUserWalletsTests
{
    private const string UserId = "user-1";

    private static UserWalletParams QueryForUser() => new()
    {
        UserId = UserId,
        businessCode = "BIZ1",
        Env = "prod",
        pageNumber = 1,
        pageSize = 20
    };

    private static PagedListResponse<List<UserWalletResponseModel>> PageOf(
        int count, int pageNumber, int pageSize)
    {
        return new PagedListResponse<List<UserWalletResponseModel>>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Count = count,
            Data = Enumerable.Range(1, Math.Min(count, pageSize))
                .Select(i => new UserWalletResponseModel { Id = i, UserId = UserId })
                .ToList()
        };
    }

    /// <summary>
    /// Drives the controller with the page the repository would have produced, and hands back
    /// the body it serialized.
    /// </summary>
    private static async Task<PagedListResponse<List<UserWalletResponseModel>>> GetBodyAsync(
        PagedListResponse<List<UserWalletResponseModel>> repositoryPage)
    {
        var walletService = new Mock<INomaOSWalletService>();
        walletService
            .Setup(x => x.GetUserWallletsByUserId(It.IsAny<UserWalletParams>()))
            .ReturnsAsync(repositoryPage);

        var controller = new UserWalletController(
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IWebhookRouter>(),
            walletService.Object);

        var result = await controller.GetUserWallet(QueryForUser());

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        return ok.Value.Should().BeOfType<PagedListResponse<List<UserWalletResponseModel>>>().Subject;
    }

    /// <summary>
    /// The controller used to rebuild the response without PageNumber or PageSize, so both
    /// serialized as 0 and TotalPages divided by zero to int.MinValue on every call.
    /// </summary>
    [Fact]
    public async Task GetUserWallet_EchoesThePagingTheRepositoryResolved()
    {
        var body = await GetBodyAsync(PageOf(count: 3, pageNumber: 1, pageSize: 20));

        body.PageNumber.Should().Be(1);
        body.PageSize.Should().Be(20);
        body.Count.Should().Be(3);
    }

    [Fact]
    public async Task GetUserWallet_ReportsAUsableTotalPages()
    {
        var body = await GetBodyAsync(PageOf(count: 25, pageNumber: 1, pageSize: 10));

        body.TotalPages.Should().Be(3);
        body.TotalPages.Should().BePositive("a negative page count means PageSize was left at 0");
    }

    [Fact]
    public async Task GetUserWallet_WithNoWallets_StillReportsSanePaging()
    {
        var body = await GetBodyAsync(PageOf(count: 0, pageNumber: 1, pageSize: 10));

        body.Count.Should().Be(0);
        body.PageSize.Should().Be(10);
        body.TotalPages.Should().Be(0);
        body.Data.Should().BeEmpty();
        body.Code.Should().Be(Constants.ResponseCodes.Failed);
    }
}
