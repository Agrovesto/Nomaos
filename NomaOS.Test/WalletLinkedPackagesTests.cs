using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace NomaOS.Test;

public class WalletLinkedPackagesTests
{
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";
    private const string BusinessCode = "BIZ1";

    private static DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DataContext(options);
    }

    private static UserWalletRepository CreateRepository(DataContext context) =>
        new(context, Mock.Of<IAWSService>());

    private static UserWalletParams Query(long walletId = 0) => new()
    {
        UserId = UserId,
        businessCode = BusinessCode,
        Env = "prod",
        Id = walletId
    };

    private static async Task<UserWallet> SeedWalletAsync(DataContext context, string userId, string businessCode = BusinessCode)
    {
        var wallet = new UserWallet
        {
            UserId = userId,
            WalletProviderId = 1,
            WalletBalance = 0,
            Currency = "NGN",
            IsActive = true,
            WalletNumber = "08000000000",
            BelongToBusinessCode = businessCode,
            IsDeleted = false,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        context.UserWallets.Add(wallet);
        await context.SaveChangesAsync();

        return wallet;
    }

    private static async Task<InputPackage> SeedPackageAsync(DataContext context, string name, long? walletId)
    {
        var package = new InputPackage
        {
            Name = name,
            InputFinancingCategoryId = 1,
            UserWalletId = walletId,
            BusinessCode = BusinessCode,
            CreatedByUserId = UserId,
            Currency = "NGN",
            IsActive = true
        };

        context.InputPackages.Add(package);
        await context.SaveChangesAsync();

        return package;
    }

    private static async Task<InputPackageSubscription> SeedSubscriptionAsync(
        DataContext context,
        long packageId,
        SubscriptionStatus status,
        params (decimal Amount, decimal AmountPaid, bool IsPaid, bool IsDeleted)[] schedules)
    {
        var subscription = new InputPackageSubscription
        {
            InputPackageId = packageId,
            SubscriberUserId = "farmer-1",
            AddedByUserId = UserId,
            Status = status,
            BusinessCode = BusinessCode,
            SubscribedOn = DateTimeOffset.UtcNow
        };

        context.InputPackageSubscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var installment = 1;
        foreach (var schedule in schedules)
        {
            context.RepaymentSchedules.Add(new RepaymentSchedule
            {
                InputPackageSubscriptionId = subscription.Id,
                InstallmentNumber = installment++,
                DueDate = DateTimeOffset.UtcNow.AddMonths(installment),
                Amount = schedule.Amount,
                AmountPaid = schedule.AmountPaid,
                IsPaid = schedule.IsPaid,
                IsDeleted = schedule.IsDeleted
            });
        }

        await context.SaveChangesAsync();

        return subscription;
    }

    [Fact]
    public async Task GetWalletLinkedPackages_CountsActiveLoansAndSumsWhatIsStillOwed()
    {
        await using var context = CreateContext();
        var wallet = await SeedWalletAsync(context, UserId);
        var package = await SeedPackageAsync(context, "Maize Wet Season", wallet.Id);

        await SeedSubscriptionAsync(context, package.Id, SubscriptionStatus.Active,
            (50000, 20000, false, false),
            (50000, 50000, true, false));

        await SeedSubscriptionAsync(context, package.Id, SubscriptionStatus.PendingDisbursement,
            (40000, 0, false, false));

        var packages = await CreateRepository(context).GetWalletLinkedPackages(Query());

        packages.Should().HaveCount(1);
        packages[0].PackageName.Should().Be("Maize Wet Season");
        packages[0].PackageId.Should().Be(package.Id);
        packages[0].WalletId.Should().Be(wallet.Id);
        packages[0].ActiveLoans.Should().Be(2);
        packages[0].OutstandingPortfolio.Should().Be(70000);
    }

    [Fact]
    public async Task GetWalletLinkedPackages_LeavesOutSettledLoansAndSupersededSchedules()
    {
        await using var context = CreateContext();
        var wallet = await SeedWalletAsync(context, UserId);
        var package = await SeedPackageAsync(context, "Rice Dry Season", wallet.Id);

        await SeedSubscriptionAsync(context, package.Id, SubscriptionStatus.Repaid,
            (30000, 30000, true, false));

        await SeedSubscriptionAsync(context, package.Id, SubscriptionStatus.Active,
            (25000, 0, false, true),
            (15000, 5000, false, false));

        var packages = await CreateRepository(context).GetWalletLinkedPackages(Query());

        packages.Should().HaveCount(1);
        packages[0].ActiveLoans.Should().Be(1);
        packages[0].OutstandingPortfolio.Should().Be(10000);
    }

    [Fact]
    public async Task GetWalletLinkedPackages_ReturnsPackagesWithNoSubscribersAtZero()
    {
        await using var context = CreateContext();
        var wallet = await SeedWalletAsync(context, UserId);
        await SeedPackageAsync(context, "Cassava Pilot", wallet.Id);

        var packages = await CreateRepository(context).GetWalletLinkedPackages(Query());

        packages.Should().HaveCount(1);
        packages[0].ActiveLoans.Should().Be(0);
        packages[0].OutstandingPortfolio.Should().Be(0);
    }

    [Fact]
    public async Task GetWalletLinkedPackages_IgnoresPackagesOnAnotherUsersWalletAndUnlinkedPackages()
    {
        await using var context = CreateContext();
        var wallet = await SeedWalletAsync(context, UserId);
        var otherWallet = await SeedWalletAsync(context, OtherUserId);

        await SeedPackageAsync(context, "Mine", wallet.Id);
        await SeedPackageAsync(context, "Theirs", otherWallet.Id);
        await SeedPackageAsync(context, "Not Yet Funded", null);

        var packages = await CreateRepository(context).GetWalletLinkedPackages(Query());

        packages.Should().ContainSingle();
        packages[0].PackageName.Should().Be("Mine");
    }

    [Fact]
    public async Task GetWalletLinkedPackages_WithAWalletId_ReturnsOnlyThatWalletsPackages()
    {
        await using var context = CreateContext();
        var firstWallet = await SeedWalletAsync(context, UserId);
        var secondWallet = await SeedWalletAsync(context, UserId);

        await SeedPackageAsync(context, "Funded By First", firstWallet.Id);
        await SeedPackageAsync(context, "Funded By Second", secondWallet.Id);

        var all = await CreateRepository(context).GetWalletLinkedPackages(Query());
        var scoped = await CreateRepository(context).GetWalletLinkedPackages(Query(secondWallet.Id));

        all.Select(x => x.PackageName).Should().BeEquivalentTo("Funded By First", "Funded By Second");
        scoped.Should().ContainSingle();
        scoped[0].PackageName.Should().Be("Funded By Second");
        scoped[0].WalletId.Should().Be(secondWallet.Id);
    }

    [Fact]
    public async Task GetWalletLinkedPackages_ForAUserWithNoWallets_ReturnsNothing()
    {
        await using var context = CreateContext();
        var otherWallet = await SeedWalletAsync(context, OtherUserId);
        await SeedPackageAsync(context, "Theirs", otherWallet.Id);

        var packages = await CreateRepository(context).GetWalletLinkedPackages(Query());

        packages.Should().BeEmpty();
    }
}
