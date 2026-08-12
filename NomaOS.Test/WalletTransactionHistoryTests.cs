using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using CoreTxnStatus = Agrovesto.Backend.Core.Enums.Wallet.WalletTransactionStatus;
using CoreTxnType = Agrovesto.Backend.Core.Enums.Wallet.WalletTransactionType;

namespace NomaOS.Test;

public class WalletTransactionHistoryTests
{
    private const long WalletId = 42;

    private static DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DataContext(options);
    }

    private static UserWalletRepository CreateRepository(DataContext context) =>
        new(context, Mock.Of<IAWSService>());

    private static async Task SeedTransactionsAsync(DataContext context)
    {
        context.UserWalletTransactions.AddRange(
            new UserWalletTransactions
            {
                UserWalletId = WalletId,
                TransactionReference = "REF-COMPLETED",
                TransactionAmount = 5000,
                BalanceAfter = 15000,
                WalletTransactionType = WalletTransactionType.Credit,
                WalletTransactionStatus = WalletTransactionStatus.Completed,
                TransactionDate = new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero)
            },
            new UserWalletTransactions
            {
                UserWalletId = WalletId,
                TransactionReference = "REF-PENDING",
                TransactionAmount = 2000,
                BalanceAfter = 13000,
                WalletTransactionType = WalletTransactionType.Debit,
                WalletTransactionStatus = WalletTransactionStatus.DebitPending,
                TransactionDate = new DateTimeOffset(2026, 8, 9, 9, 0, 0, TimeSpan.Zero)
            },
            new UserWalletTransactions
            {
                UserWalletId = WalletId,
                TransactionReference = "REF-FAILED",
                TransactionAmount = 1000,
                BalanceAfter = 13000,
                WalletTransactionType = WalletTransactionType.Debit,
                WalletTransactionStatus = WalletTransactionStatus.Failed,
                TransactionDate = new DateTimeOffset(2026, 8, 8, 9, 0, 0, TimeSpan.Zero)
            });

        await context.SaveChangesAsync();
    }

    private static ListQueryParams QueryFor(CoreTxnStatus? status = null) => new()
    {
        Id = WalletId,
        PageNumber = 1,
        PageSize = 10,
        TransactionStatus = status
    };

    [Fact]
    public async Task GetUserWalletTransaction_ProjectsStatusOntoEveryTransaction()
    {
        using var context = CreateContext();
        await SeedTransactionsAsync(context);

        var result = await CreateRepository(context).GetUserWalletTransaction(QueryFor());

        result.Data.Should().HaveCount(3);
        result.Data.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Status));

        // Ordered by TransactionDate descending, so the completed one leads.
        result.Data[0].TransactionReference.Should().Be("REF-COMPLETED");
        result.Data[0].Status.Should().Be(nameof(WalletTransactionStatus.Completed));
        result.Data[1].Status.Should().Be(nameof(WalletTransactionStatus.DebitPending));
        result.Data[2].Status.Should().Be(nameof(WalletTransactionStatus.Failed));
    }

    [Fact]
    public async Task GetUserWalletTransaction_WithStatusFilter_ReturnsOnlyThatStatus()
    {
        using var context = CreateContext();
        await SeedTransactionsAsync(context);

        var result = await CreateRepository(context)
            .GetUserWalletTransaction(QueryFor(CoreTxnStatus.Failed));

        result.Count.Should().Be(1);
        result.Data.Should().ContainSingle()
            .Which.TransactionReference.Should().Be("REF-FAILED");
    }

    [Fact]
    public async Task GetUserWalletTransaction_WithStatusFilterMatchingNothing_ReturnsEmpty()
    {
        using var context = CreateContext();
        await SeedTransactionsAsync(context);

        var result = await CreateRepository(context)
            .GetUserWalletTransaction(QueryFor(CoreTxnStatus.Reversed));

        result.Count.Should().Be(0);
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserWalletTransaction_WithoutStatusFilter_StillReturnsEveryStatus()
    {
        using var context = CreateContext();
        await SeedTransactionsAsync(context);

        var result = await CreateRepository(context).GetUserWalletTransaction(QueryFor());

        result.Count.Should().Be(3);
        result.Data!.Select(x => x.Status).Should().BeEquivalentTo(new[]
        {
            nameof(WalletTransactionStatus.Completed),
            nameof(WalletTransactionStatus.DebitPending),
            nameof(WalletTransactionStatus.Failed)
        });
    }

    [Fact]
    public async Task GetUserWalletTransaction_CombinesStatusAndTypeFilters()
    {
        using var context = CreateContext();
        await SeedTransactionsAsync(context);

        var query = QueryFor(CoreTxnStatus.DebitPending);
        query.TransactionType = CoreTxnType.Debit;

        var result = await CreateRepository(context).GetUserWalletTransaction(query);

        result.Count.Should().Be(1);
        result.Data.Should().ContainSingle()
            .Which.TransactionReference.Should().Be("REF-PENDING");
    }

    /// <summary>
    /// The Core enum is a hand-maintained mirror of the entity one and the repository casts
    /// between them by ordinal, so any drift would silently filter on the wrong status.
    /// </summary>
    [Fact]
    public void CoreAndEntityTransactionStatusEnums_AgreeOnEveryNameAndOrdinal()
    {
        var core = Enum.GetValues<CoreTxnStatus>()
            .ToDictionary(x => x.ToString(), x => (int)x);

        var entity = Enum.GetValues<WalletTransactionStatus>()
            .ToDictionary(x => x.ToString(), x => (int)x);

        core.Should().BeEquivalentTo(entity);
    }
}
