using Agrovesto.Backend.API.Controllers.V2.Internal;
using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace NomaOS.Test;

public class WalletTransactionPagingTests
{
    private const string UserId = "user-1";

    private static ListQueryParams QueryFor(int pageNumber, int pageSize) => new()
    {
        UserId = UserId,
        businessCode = "BIZ1",
        Id = 42,
        PageNumber = pageNumber,
        PageSize = pageSize
    };

    private static PagedListResponse<List<FarmerTransactionDto>> PageOf(
        int count, int pageNumber, int pageSize)
    {
        return new PagedListResponse<List<FarmerTransactionDto>>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Count = count,
            Data = Enumerable.Range(1, Math.Min(count, pageSize))
                .Select(i => new FarmerTransactionDto
                {
                    TransactionId = i,
                    TransactionReference = $"REF-{i}",
                    Status = "Completed"
                })
                .ToList()
        };
    }

    private static async Task<PagedListResponse<List<FarmerTransactionDto>>> GetBodyAsync(
        PagedListResponse<List<FarmerTransactionDto>> repositoryPage,
        ListQueryParams query)
    {
        var walletService = new Mock<INomaOSWalletService>();
        walletService
            .Setup(x => x.GetUserWalletTransaction(It.IsAny<ListQueryParams>()))
            .ReturnsAsync(repositoryPage);

        var controller = new UserWalletController(
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IWebhookRouter>(),
            walletService.Object);

        var result = await controller.GetUserWallet(query);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        return ok.Value.Should().BeOfType<PagedListResponse<List<FarmerTransactionDto>>>().Subject;
    }

    /// <summary>
    /// PageSize used to be hardcoded to 10 while the repository paged by the requested size,
    /// so asking for 50 returned 50 rows but reported 10, and totalPages was computed off the
    /// wrong divisor.
    /// </summary>
    [Fact]
    public async Task GetUserWalletTransaction_EchoesTheRequestedPageSize()
    {
        var body = await GetBodyAsync(
            PageOf(count: 120, pageNumber: 2, pageSize: 50),
            QueryFor(pageNumber: 2, pageSize: 50));

        body.PageSize.Should().Be(50);
        body.PageNumber.Should().Be(2);
        body.Count.Should().Be(120);
    }

    [Fact]
    public async Task GetUserWalletTransaction_ReportsTotalPagesAgainstTheRealPageSize()
    {
        var body = await GetBodyAsync(
            PageOf(count: 120, pageNumber: 1, pageSize: 50),
            QueryFor(pageNumber: 1, pageSize: 50));

        // 120 rows at 50 per page is 3 pages. Against the old hardcoded 10 it reported 12.
        body.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetUserWalletTransaction_WithNoRows_StillReportsSanePaging()
    {
        var body = await GetBodyAsync(
            PageOf(count: 0, pageNumber: 1, pageSize: 10),
            QueryFor(pageNumber: 1, pageSize: 10));

        body.Count.Should().Be(0);
        body.PageSize.Should().Be(10);
        body.TotalPages.Should().Be(0);
        body.Data.Should().BeEmpty();
        body.Code.Should().Be(Constants.ResponseCodes.Failed);
    }
}
