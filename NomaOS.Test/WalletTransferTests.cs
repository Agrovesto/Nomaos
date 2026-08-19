using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Interfaces.Repositories;
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

public class WalletTransferTests
{
    private const string SenderId = "user-1";
    private const string RecipientId = "user-2";
    private const string BusinessCode = "BIZ1";
    private const long ProviderId = 3;
    private const long OtherProviderId = 4;
    private const string SourceNumber = "2349012981234";
    private const string DestinationNumber = "2349087654321";
    private const string Pin = "1234";

    private sealed class StubTransferService : IGenericWalletService
    {
        private readonly Func<TransferRequest, WalletResponseService<PaymentResponse>> _transfer;
        private readonly Func<WalletTopUpProviderRequest, WalletResponseService<PaymentResponse>> _debit;

        public StubTransferService(
            Func<TransferRequest, WalletResponseService<PaymentResponse>>? transfer = null,
            Func<WalletTopUpProviderRequest, WalletResponseService<PaymentResponse>>? debit = null)
        {
            _transfer = transfer ?? (r => WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Successful", Reference = r.Reference, ProviderReference = "PRV-1" },
                "Transfer completed"));

            _debit = debit ?? (r => WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Successful", Reference = r.TransactionReference, ProviderReference = "DBT-1" },
                "Debit completed"));
        }

        public TransferRequest? LastRequest { get; private set; }
        public int CallCount { get; private set; }
        public int DebitCallCount { get; private set; }

        public string ProviderKey => "Momo";
        public bool RequiresPreDebit => true;

        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTransferAsync(
            TransferRequest request, WalletProviderParameterModel[] parameters, string env)
        {
            LastRequest = request;
            CallCount++;
            return Task.FromResult(_transfer(request));
        }

        public Task<WalletResponseService<PaymentResponse>> DebitWalletAsync(
            WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string env)
        {
            DebitCallCount++;
            return Task.FromResult(_debit(request));
        }

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

    private static async Task<(UserWallet source, UserWallet destination)> SeedWalletsAsync(
        DataContext context,
        decimal sourceBalance = 5000m,
        decimal destinationBalance = 100m,
        long destinationProviderId = ProviderId)
    {
        context.WalletProviders.Add(new WalletProvider
        {
            ProviderId = ProviderId,
            Code = "Momo",
            Name = "MTN Momo",
            Status = "Active"
        });

        context.WalletProviders.Add(new WalletProvider
        {
            ProviderId = OtherProviderId,
            Code = "Etransact",
            Name = "Etransact",
            Status = "Active"
        });

        var source = new UserWallet
        {
            UserId = SenderId,
            WalletProviderId = ProviderId,
            WalletBalance = sourceBalance,
            Currency = "NGN",
            IsActive = true,
            WalletNumber = SourceNumber,
            BelongToBusinessCode = BusinessCode,
            IsDeleted = false,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        var destination = new UserWallet
        {
            UserId = RecipientId,
            WalletProviderId = destinationProviderId,
            WalletBalance = destinationBalance,
            Currency = "NGN",
            IsActive = true,
            WalletNumber = DestinationNumber,
            BelongToBusinessCode = BusinessCode,
            IsDeleted = false,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        context.UserWallets.AddRange(source, destination);
        await context.SaveChangesAsync();

        context.WalletSecurities.Add(new WalletSecurity
        {
            UserWalletId = source.Id,
            Txn = Pin,
            TransactionPinHash = BCrypt.Net.BCrypt.HashPassword(Pin)
        });
        await context.SaveChangesAsync();

        return (source, destination);
    }

    private static NomaOSWalletService CreateService(DataContext context, IGenericWalletService provider)
    {
        var providerRepo = new Mock<IWalletProviderRepository>();

        providerRepo
            .Setup(x => x.Get(ProviderId))
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
            .Setup(x => x.GetUserById(SenderId))
            .ReturnsAsync((new UserModel { Id = SenderId, Name = "Ada Lovelace" }, true, string.Empty));
        onboardingRepository
            .Setup(x => x.GetUserById(RecipientId))
            .ReturnsAsync((new UserModel { Id = RecipientId, Name = "Charles Babbage" }, true, string.Empty));

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
            Mock.Of<IConfiguration>());
    }

    private static NomaOSWalletService CreateCrossProviderService(
        DataContext context,
        IGenericWalletService sourceProvider,
        IGenericWalletService destinationProvider)
    {
        var providerRepo = new Mock<IWalletProviderRepository>();

        providerRepo
            .Setup(x => x.Get(ProviderId))
            .ReturnsAsync(new WalletProviderModel { ProviderId = ProviderId, Code = "Momo", Name = "MTN Momo" });

        providerRepo
            .Setup(x => x.Get(OtherProviderId))
            .ReturnsAsync(new WalletProviderModel { ProviderId = OtherProviderId, Code = "Etransact", Name = "Etransact" });

        providerRepo
            .Setup(x => x.GetProvidersForBusiness(BusinessCode))
            .ReturnsAsync(new[]
            {
                new BusinessWalletProviderModel
                {
                    BusinessCode = BusinessCode,
                    ProviderId = ProviderId,
                    WalletProvider = new WalletProviderModel { ProviderId = ProviderId, Code = "Momo" }
                },
                new BusinessWalletProviderModel
                {
                    BusinessCode = BusinessCode,
                    ProviderId = OtherProviderId,
                    WalletProvider = new WalletProviderModel { ProviderId = OtherProviderId, Code = "Etransact" }
                }
            });

        providerRepo
            .Setup(x => x.GetParametersByProvider(It.IsAny<long>(), BusinessCode, It.IsAny<string>()))
            .ReturnsAsync(new[]
            {
                new WalletProviderParameterModel { Key = "BaseUrl", Value = "https://example.test", ProviderId = ProviderId }
            });

        var factory = new Mock<IWalletServiceFactory>();
        factory.Setup(x => x.GetService("Momo")).Returns(sourceProvider);
        factory.Setup(x => x.GetService("Etransact")).Returns(destinationProvider);

        var onboardingRepository = new Mock<IOnboardingRepository>();
        onboardingRepository
            .Setup(x => x.GetUserById(SenderId))
            .ReturnsAsync((new UserModel { Id = SenderId, Name = "Ada Lovelace" }, true, string.Empty));
        onboardingRepository
            .Setup(x => x.GetUserById(RecipientId))
            .ReturnsAsync((new UserModel { Id = RecipientId, Name = "Charles Babbage" }, true, string.Empty));

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
            Mock.Of<IConfiguration>());
    }

    private static WalletQueryParams Query() => new()
    {
        UserId = SenderId,
        businessCode = BusinessCode,
        env = "Demo"
    };

    private static WalletTransferRequest TransferOf(
        long sourceWalletId,
        decimal amount = 500m,
        string destination = DestinationNumber) => new()
        {
            SourceWalletId = sourceWalletId,
            DestinationWalletNumber = destination,
            Amount = amount,
            TransactionPin = Pin,
            Description = "Rent"
        };

    [Fact]
    public async Task TransferBetweenWallets_MovesTheMoneyAndReturnsAReceipt()
    {
        using var context = CreateContext();
        var (source, destination) = await SeedWalletsAsync(context);

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, message, data) = await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        status.Should().BeTrue();
        message.Should().Be("Transfer completed successfully");

        data.Should().NotBeNull();
        data!.Amount.Should().Be(500m);
        data.SourceWalletNumber.Should().Be(SourceNumber);
        data.DestinationWalletNumber.Should().Be(DestinationNumber);
        data.SourceBalanceAfter.Should().Be(4500m);
        data.ProviderReference.Should().Be("PRV-1");

        var wallets = await context.UserWallets.ToListAsync();
        wallets.Single(x => x.Id == source.Id).WalletBalance.Should().Be(4500m);
        wallets.Single(x => x.Id == destination.Id).WalletBalance.Should().Be(600m);
    }

    [Fact]
    public async Task TransferBetweenWallets_SendsSenderAndRecipientNamesToTheProvider()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        provider.LastRequest!.SenderName.Should().Be("Ada Lovelace");
        provider.LastRequest.SenderAccountNumber.Should().Be(SourceNumber);
        provider.LastRequest.RecipientName.Should().Be("Charles Babbage");
        provider.LastRequest.AccountNumber.Should().Be(DestinationNumber);
    }

    [Fact]
    public async Task TransferBetweenWallets_WritesBothLedgerLegs()
    {
        using var context = CreateContext();
        var (source, destination) = await SeedWalletsAsync(context);

        var service = CreateService(context, new StubTransferService());

        var (_, _, data) = await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        var rows = await context.UserWalletTransactions.ToListAsync();
        rows.Should().HaveCount(2);

        var debit = rows.Single(x => x.WalletTransactionType == WalletTransactionType.Debit);
        debit.UserWalletId.Should().Be(source.Id);
        debit.TransactionReference.Should().Be($"{data!.TransactionReference}-DB");
        debit.WalletTransactionStatus.Should().Be(WalletTransactionStatus.Completed);

        var credit = rows.Single(x => x.WalletTransactionType == WalletTransactionType.Credit);
        credit.UserWalletId.Should().Be(destination.Id);
        credit.TransactionReference.Should().Be($"{data.TransactionReference}-CR");
        credit.WalletTransactionStatus.Should().Be(WalletTransactionStatus.Completed);
    }

    [Fact]
    public async Task TransferBetweenWallets_WhenTheProviderRejects_LeavesBothBalancesUntouched()
    {
        using var context = CreateContext();
        var (source, destination) = await SeedWalletsAsync(context);

        var provider = new StubTransferService(
            _ => WalletResponseService<PaymentResponse>.Failed("Provider declined the transfer", 400));

        var service = CreateService(context, provider);

        var (status, message, data) = await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        status.Should().BeFalse();
        message.Should().Be("Provider declined the transfer");
        data.Should().BeNull();

        var wallets = await context.UserWallets.ToListAsync();
        wallets.Single(x => x.Id == source.Id).WalletBalance.Should().Be(5000m);
        wallets.Single(x => x.Id == destination.Id).WalletBalance.Should().Be(100m);

        var rows = await context.UserWalletTransactions.ToListAsync();
        rows.Should().HaveCount(2);
        rows.Should().OnlyContain(x => x.WalletTransactionStatus == WalletTransactionStatus.Failed);
    }

    [Fact]
    public async Task TransferBetweenWallets_WithInsufficientBalance_NeverCallsTheProvider()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context, sourceBalance: 100m);

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, message, _) = await service.TransferBetweenWallets(TransferOf(source.Id, amount: 500m), Query());

        status.Should().BeFalse();
        message.Should().Be("Insufficient balance");

        provider.CallCount.Should().Be(0);
        (await context.UserWalletTransactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task TransferBetweenWallets_WithTheWrongPin_NeverCallsTheProvider()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var request = TransferOf(source.Id);
        request.TransactionPin = "9999";

        var (status, _, _) = await service.TransferBetweenWallets(request, Query());

        status.Should().BeFalse();
        provider.CallCount.Should().Be(0);
        (await context.UserWalletTransactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task TransferBetweenWallets_ToTheSameWallet_IsRejected()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, message, _) = await service.TransferBetweenWallets(
            TransferOf(source.Id, destination: SourceNumber), Query());

        status.Should().BeFalse();
        message.Should().Be("Cannot transfer to the same wallet");
        provider.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task TransferBetweenWallets_AcrossProviders_DebitsTheSourceAndCreditsTheDestination()
    {
        using var context = CreateContext();
        var (source, destination) = await SeedWalletsAsync(context, destinationProviderId: OtherProviderId);

        var sourceProvider = new StubTransferService();
        var destinationProvider = new StubTransferService();
        var service = CreateCrossProviderService(context, sourceProvider, destinationProvider);

        var (status, message, data) = await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        status.Should().BeTrue();
        message.Should().Be("Transfer completed successfully");
        data!.Status.Should().Be("Successful");

        sourceProvider.DebitCallCount.Should().Be(1);
        destinationProvider.CallCount.Should().Be(1);

        var wallets = await context.UserWallets.ToListAsync();
        wallets.Single(x => x.Id == source.Id).WalletBalance.Should().Be(4500m);
        wallets.Single(x => x.Id == destination.Id).WalletBalance.Should().Be(600m);
    }

    [Fact]
    public async Task TransferBetweenWallets_AcrossProviders_WhenTheDebitFails_CreditsNothingAndMovesNoMoney()
    {
        using var context = CreateContext();
        var (source, destination) = await SeedWalletsAsync(context, destinationProviderId: OtherProviderId);

        var sourceProvider = new StubTransferService(
            debit: _ => WalletResponseService<PaymentResponse>.Failed("Source provider declined the debit", 400));
        var destinationProvider = new StubTransferService();

        var service = CreateCrossProviderService(context, sourceProvider, destinationProvider);

        var (status, message, _) = await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        status.Should().BeFalse();
        message.Should().Be("Source provider declined the debit");

        destinationProvider.CallCount.Should().Be(0);

        var wallets = await context.UserWallets.ToListAsync();
        wallets.Single(x => x.Id == source.Id).WalletBalance.Should().Be(5000m);
        wallets.Single(x => x.Id == destination.Id).WalletBalance.Should().Be(100m);
    }

    [Fact]
    public async Task TransferBetweenWallets_AcrossProviders_WhenTheCreditFails_ReversesTheDebit()
    {
        using var context = CreateContext();
        var (source, destination) = await SeedWalletsAsync(context, destinationProviderId: OtherProviderId);

        var sourceProvider = new StubTransferService();
        var destinationProvider = new StubTransferService(
            transfer: _ => WalletResponseService<PaymentResponse>.Failed("Destination provider unavailable", 503));

        var service = CreateCrossProviderService(context, sourceProvider, destinationProvider);

        var (status, message, data) = await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        status.Should().BeFalse();
        message.Should().Contain("has been reversed");
        data!.Status.Should().Be("Reversed");

        sourceProvider.CallCount.Should().Be(1);

        var wallets = await context.UserWallets.ToListAsync();
        wallets.Single(x => x.Id == source.Id).WalletBalance.Should().Be(5000m);
        wallets.Single(x => x.Id == destination.Id).WalletBalance.Should().Be(100m);

        var rows = await context.UserWalletTransactions.ToListAsync();
        rows.Should().Contain(x => x.TransactionReference.EndsWith("-RV")
            && x.WalletTransactionStatus == WalletTransactionStatus.Completed);
    }

    [Fact]
    public async Task TransferBetweenWallets_AcrossProviders_WhenTheReversalAlsoFails_FlagsForReconciliation()
    {
        using var context = CreateContext();
        var (source, destination) = await SeedWalletsAsync(context, destinationProviderId: OtherProviderId);

        var sourceProvider = new StubTransferService(
            transfer: _ => WalletResponseService<PaymentResponse>.Failed("Reversal rejected", 400));
        var destinationProvider = new StubTransferService(
            transfer: _ => WalletResponseService<PaymentResponse>.Failed("Destination provider unavailable", 503));

        var service = CreateCrossProviderService(context, sourceProvider, destinationProvider);

        var (status, message, data) = await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        status.Should().BeFalse();
        message.Should().Contain("could not be automatically reversed");
        data.Should().BeNull();

        var log = await context.WalletReconciliationLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();

        var wallets = await context.UserWallets.ToListAsync();
        wallets.Single(x => x.Id == source.Id).WalletBalance.Should().Be(4500m);
        wallets.Single(x => x.Id == destination.Id).WalletBalance.Should().Be(100m);
    }

    [Fact]
    public async Task TransferBetweenWallets_AcrossProviders_WhenTheDebitIsPending_MovesNoMoney()
    {
        using var context = CreateContext();
        var (source, destination) = await SeedWalletsAsync(context, destinationProviderId: OtherProviderId);

        var sourceProvider = new StubTransferService(
            debit: r => WalletResponseService<PaymentResponse>.Pending(
                new PaymentResponse { Status = "Pending", Reference = r.TransactionReference }, "Pending"));
        var destinationProvider = new StubTransferService();

        var service = CreateCrossProviderService(context, sourceProvider, destinationProvider);

        var (status, _, data) = await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        status.Should().BeTrue();
        data!.Status.Should().Be("Pending");

        destinationProvider.CallCount.Should().Be(0);

        var wallets = await context.UserWallets.ToListAsync();
        wallets.Single(x => x.Id == source.Id).WalletBalance.Should().Be(5000m);
        wallets.Single(x => x.Id == destination.Id).WalletBalance.Should().Be(100m);
    }

    [Fact]
    public async Task TransferBetweenWallets_ToAnUnknownWallet_IsRejected()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, message, _) = await service.TransferBetweenWallets(
            TransferOf(source.Id, destination: "0000000000"), Query());

        status.Should().BeFalse();
        message.Should().Be("Destination wallet not found");
        provider.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task TransferBetweenWallets_WhenTheProviderIsPending_LeavesBalancesAndRowsUnsettled()
    {
        using var context = CreateContext();
        var (source, destination) = await SeedWalletsAsync(context);

        var provider = new StubTransferService(
            r => WalletResponseService<PaymentResponse>.Pending(
                new PaymentResponse { Status = "Pending", Reference = r.Reference, ProviderReference = "PRV-9" },
                "Transfer pending - transaction status query required"));

        var service = CreateService(context, provider);

        var (status, message, data) = await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        status.Should().BeTrue();
        message.Should().Be("Your transfer has been accepted and is awaiting confirmation.");
        data!.Status.Should().Be("Pending");

        var wallets = await context.UserWallets.ToListAsync();
        wallets.Single(x => x.Id == source.Id).WalletBalance.Should().Be(5000m);
        wallets.Single(x => x.Id == destination.Id).WalletBalance.Should().Be(100m);

        var rows = await context.UserWalletTransactions.ToListAsync();
        rows.Should().HaveCount(2);
        rows.Should().NotContain(x => x.WalletTransactionStatus == WalletTransactionStatus.Completed);
    }

    [Fact]
    public async Task TransferBetweenWallets_WhenPending_IsFlaggedForReconciliation()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubTransferService(
            r => WalletResponseService<PaymentResponse>.Pending(
                new PaymentResponse { Status = "Pending", Reference = r.Reference }, "Pending"));

        var service = CreateService(context, provider);

        var (_, _, data) = await service.TransferBetweenWallets(TransferOf(source.Id), Query());

        var log = await context.WalletReconciliationLogs
            .FirstOrDefaultAsync(x => x.DebitReference == $"{data!.TransactionReference}-DB");

        log.Should().NotBeNull();
    }

    [Fact]
    public async Task TransferBetweenWallets_ReplayingAClientReference_DoesNotMoveMoneyTwice()
    {
        using var context = CreateContext();
        var (source, destination) = await SeedWalletsAsync(context);

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var request = TransferOf(source.Id);
        request.ClientReference = "checkout-0001";

        var first = await service.TransferBetweenWallets(request, Query());
        first.status.Should().BeTrue();

        var replay = await service.TransferBetweenWallets(request, Query());

        replay.status.Should().BeTrue();
        replay.message.Should().Be("A transfer with this reference has already been submitted");

        provider.CallCount.Should().Be(1);

        var wallets = await context.UserWallets.ToListAsync();
        wallets.Single(x => x.Id == source.Id).WalletBalance.Should().Be(4500m);
        wallets.Single(x => x.Id == destination.Id).WalletBalance.Should().Be(600m);

        (await context.UserWalletTransactions.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task TransferBetweenWallets_UsesTheClientReferenceAsTheTransactionReference()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var service = CreateService(context, new StubTransferService());

        var request = TransferOf(source.Id);
        request.ClientReference = "checkout-0002";

        var (_, _, data) = await service.TransferBetweenWallets(request, Query());

        data!.TransactionReference.Should().Be("checkout-0002");

        var references = await context.UserWalletTransactions.Select(x => x.TransactionReference).ToListAsync();
        references.Should().BeEquivalentTo(new[] { "checkout-0002-DB", "checkout-0002-CR" });
    }

    [Fact]
    public async Task TransferBetweenWallets_FromAWalletTheCallerDoesNotOwn_IsRejected()
    {
        using var context = CreateContext();
        var (_, destination) = await SeedWalletsAsync(context);

        var provider = new StubTransferService();
        var service = CreateService(context, provider);

        var (status, message, _) = await service.TransferBetweenWallets(
            TransferOf(destination.Id), Query());

        status.Should().BeFalse();
        message.Should().Be("Source wallet not found");
        provider.CallCount.Should().Be(0);
    }
}
