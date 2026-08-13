using Agrovesto.Backend.API.Controllers.V2.Internal;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace NomaOS.Test;

public class WalletSummaryTests
{
    private const string UserId = "user-1";

    private static UserWalletController CreateController(FarmerFinancialSummaryDto summary)
    {
        var walletService = new Mock<INomaOSWalletService>();
        walletService
            .Setup(x => x.GetUserFinancialSummary(It.IsAny<UserWalletParams>()))
            .ReturnsAsync(summary);

        return new UserWalletController(
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IWebhookRouter>(),
            walletService.Object);
    }

    private static UserWalletParams QueryForUser() => new()
    {
        UserId = UserId,
        businessCode = "BIZ1",
        Env = "prod"
    };

    /// <summary>
    /// The repository returns a null NextRepaymentDate for anyone with no input financing.
    /// This used to be cast straight to a non-nullable DateTimeOffset, which threw and was
    /// swallowed as a generic 400, so the endpoint failed for every user without a subscription.
    /// </summary>
    [Fact]
    public async Task GetUserFinancialSummary_WithNoUpcomingRepayment_ReturnsSummaryWithNullDueDate()
    {
        var controller = CreateController(new FarmerFinancialSummaryDto
        {
            TotalWalletBalance = 47000,
            OutstandingInputDebt = 0,
            NextRepaymentAmount = 0,
            NextRepaymentDate = null
        });

        var result = await controller.GetUserFinancialSummary(QueryForUser());

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = ok.Value.Should().BeOfType<WalletDashboardResponseModel>().Subject;

        dashboard.NextDuePaymentDate.Should().BeNull();
        dashboard.TotalWalletBalance.Should().Be(47000);
        dashboard.OutStandingInputDebt.Should().Be(0);
        dashboard.NextDuePaymentAmount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserFinancialSummary_WithUpcomingRepayment_PassesTheDateThrough()
    {
        var dueDate = new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero);

        var controller = CreateController(new FarmerFinancialSummaryDto
        {
            TotalWalletBalance = 47000,
            OutstandingInputDebt = 125000,
            NextRepaymentAmount = 25000,
            NextRepaymentDate = dueDate
        });

        var result = await controller.GetUserFinancialSummary(QueryForUser());

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = ok.Value.Should().BeOfType<WalletDashboardResponseModel>().Subject;

        dashboard.NextDuePaymentDate.Should().Be(dueDate);
        dashboard.OutStandingInputDebt.Should().Be(125000);
        dashboard.NextDuePaymentAmount.Should().Be(25000);
    }
}
