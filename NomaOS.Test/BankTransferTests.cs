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

public class BankTransferTests
{
    private const string SenderId = "user-1";
    private const string OtherUserId = "user-2";
    private const string BusinessCode = "BIZ1";
    private const long ProviderId = 3;
    private const string SourceNumber = "2349012981234";
    private const string OtherWalletNumber = "2349087654321";
    private const string BankAccountNumber = "0123456789";
    private const string BankCode = "058";
    private const string BankAccountName = "Ngozi Farms Ltd";
    private const string Pin = "1234";

    private sealed class StubBankService : IGenericWalletService
    {
        private readonly Func<TransferRequest, WalletResponseService<PaymentResponse>> _transfer;

        public StubBankService(
            Func<TransferRequest, WalletResponseService<PaymentResponse>>? transfer = null,
            bool supportsBankTransfer = true)
        {
            SupportsBankTransfer = supportsBankTransfer;

            _transfer = transfer ?? (r => WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Successful", Reference = r.Reference, ProviderReference = "PRV-1" },
                "Transfer completed"));
        }

        public TransferRequest? LastRequest { get; private set; }
        public int CallCount { get; private set; }

        public string ProviderKey => "Momo";
        public bool RequiresPreDebit => true;
        public bool SupportsBankTransfer { get; }

        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTransferAsync(
            TransferRequest request, WalletProviderParameterModel[] parameters, string env)
        {
            LastRequest = request;
            CallCount++;
            return Task.FromResult(_transfer(request));
        }

        public Task<WalletResponseService<PaymentResponse>> DebitWalletAsync(
            WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string env)
            => throw new NotSupportedException();

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

    private static async Task<(UserWallet source, UserWallet other)> SeedWalletsAsync(
        DataContext context,
        decimal sourceBalance = 5000m)
    {
        context.WalletProviders.Add(new WalletProvider
        {
            ProviderId = ProviderId,
            Code = "Momo",
            Name = "MTN Momo",
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

        var other = new UserWallet
        {
            UserId = OtherUserId,
            WalletProviderId = ProviderId,
            WalletBalance = 100m,
            Currency = "NGN",
            IsActive = true,
            WalletNumber = OtherWalletNumber,
            BelongToBusinessCode = BusinessCode,
            IsDeleted = false,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        context.UserWallets.AddRange(source, other);
        await context.SaveChangesAsync();

        context.WalletSecurities.Add(new WalletSecurity
        {
            UserWalletId = source.Id,
            Txn = Pin,
            TransactionPinHash = BCrypt.Net.BCrypt.HashPassword(Pin)
        });
        await context.SaveChangesAsync();

        return (source, other);
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

    private static BankTransferRequest TransferOf(
        long sourceWalletId,
        decimal amount = 500m,
        string destination = BankAccountNumber) => new()
        {
            SourceWalletId = sourceWalletId,
            DestinationAccountNumber = destination,
            BankCode = BankCode,
            DestinationAccountName = BankAccountName,
            Amount = amount,
            TransactionPin = Pin,
            Description = "Input payout"
        };

    [Fact]
    public async Task TransferToBank_DebitsTheWalletAndReturnsAReceipt()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var service = CreateService(context, new StubBankService());

        var (status, message, data) = await service.TransferToBank(TransferOf(source.Id), Query());

        status.Should().BeTrue();
        message.Should().Be("Transfer completed successfully");

        data.Should().NotBeNull();
        data!.Amount.Should().Be(500m);
        data.SourceWalletNumber.Should().Be(SourceNumber);
        data.DestinationAccountNumber.Should().Be(BankAccountNumber);
        data.BankCode.Should().Be(BankCode);
        data.DestinationAccountName.Should().Be(BankAccountName);
        data.SourceBalanceAfter.Should().Be(4500m);
        data.ProviderReference.Should().Be("PRV-1");

        var wallet = await context.UserWallets.SingleAsync(x => x.Id == source.Id);
        wallet.WalletBalance.Should().Be(4500m);
    }

    [Fact]
    public async Task TransferToBank_WritesOnlyADebitLeg()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var service = CreateService(context, new StubBankService());

        var (_, _, data) = await service.TransferToBank(TransferOf(source.Id), Query());

        var rows = await context.UserWalletTransactions.ToListAsync();
        rows.Should().HaveCount(1);

        var debit = rows.Single();
        debit.WalletTransactionType.Should().Be(WalletTransactionType.Debit);
        debit.UserWalletId.Should().Be(source.Id);
        debit.TransactionReference.Should().Be($"{data!.TransactionReference}-DB");
        debit.WalletTransactionStatus.Should().Be(WalletTransactionStatus.Completed);
    }

    [Fact]
    public async Task TransferToBank_SendsTheBankCodeAndAccountNameToTheProvider()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubBankService();
        var service = CreateService(context, provider);

        await service.TransferToBank(TransferOf(source.Id), Query());

        provider.LastRequest!.AccountNumber.Should().Be(BankAccountNumber);
        provider.LastRequest.BankCode.Should().Be(BankCode);
        provider.LastRequest.RecipientName.Should().Be(BankAccountName);
        provider.LastRequest.SenderName.Should().Be("Ada Lovelace");
        provider.LastRequest.SenderAccountNumber.Should().Be(SourceNumber);
    }

    [Fact]
    public async Task TransferToBank_WhenTheProviderRejects_LeavesTheBalanceUntouched()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubBankService(
            _ => WalletResponseService<PaymentResponse>.Failed("Provider declined the transfer", 400));

        var service = CreateService(context, provider);

        var (status, message, data) = await service.TransferToBank(TransferOf(source.Id), Query());

        status.Should().BeFalse();
        message.Should().Be("Provider declined the transfer");
        data.Should().BeNull();

        var wallet = await context.UserWallets.SingleAsync(x => x.Id == source.Id);
        wallet.WalletBalance.Should().Be(5000m);

        var rows = await context.UserWalletTransactions.ToListAsync();
        rows.Should().ContainSingle();
        rows.Single().WalletTransactionStatus.Should().Be(WalletTransactionStatus.Failed);
    }

    [Fact]
    public async Task TransferToBank_WhenTheProviderIsPending_HoldsTheBalanceAndFlagsForReconciliation()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubBankService(
            r => WalletResponseService<PaymentResponse>.Pending(
                new PaymentResponse { Status = "Pending", Reference = r.Reference, ProviderReference = "PRV-2" },
                "Awaiting confirmation"));

        var service = CreateService(context, provider);

        var (status, _, data) = await service.TransferToBank(TransferOf(source.Id), Query());

        status.Should().BeTrue();
        data!.Status.Should().Be("Pending");

        var wallet = await context.UserWallets.SingleAsync(x => x.Id == source.Id);
        wallet.WalletBalance.Should().Be(5000m);

        var logs = await context.WalletReconciliationLogs.ToListAsync();
        logs.Should().ContainSingle();
        logs.Single().DebitReference.Should().Be($"{data.TransactionReference}-DB");
    }

    [Fact]
    public async Task TransferToBank_WithInsufficientBalance_NeverCallsTheProvider()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context, sourceBalance: 100m);

        var provider = new StubBankService();
        var service = CreateService(context, provider);

        var (status, message, _) = await service.TransferToBank(TransferOf(source.Id, amount: 500m), Query());

        status.Should().BeFalse();
        message.Should().Be("Insufficient balance");

        provider.CallCount.Should().Be(0);
        (await context.UserWalletTransactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task TransferToBank_WithTheWrongPin_NeverCallsTheProvider()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubBankService();
        var service = CreateService(context, provider);

        var request = TransferOf(source.Id);
        request.TransactionPin = "9999";

        var (status, _, _) = await service.TransferToBank(request, Query());

        status.Should().BeFalse();
        provider.CallCount.Should().Be(0);
        (await context.UserWalletTransactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task TransferToBank_WhenTheProviderCannotReachBanks_IsRejected()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubBankService(supportsBankTransfer: false);
        var service = CreateService(context, provider);

        var (status, message, _) = await service.TransferToBank(TransferOf(source.Id), Query());

        status.Should().BeFalse();
        message.Should().Be("Momo wallets cannot pay out to a bank account");

        provider.CallCount.Should().Be(0);
        (await context.UserWalletTransactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task TransferToBank_ToAnAccountThatIsAPlatformWallet_IsRejected()
    {
        using var context = CreateContext();
        var (source, other) = await SeedWalletsAsync(context);

        var provider = new StubBankService();
        var service = CreateService(context, provider);

        var (status, message, _) = await service.TransferToBank(
            TransferOf(source.Id, destination: other.WalletNumber), Query());

        status.Should().BeFalse();
        message.Should().Contain("wallet transfer endpoint");

        provider.CallCount.Should().Be(0);
        (await context.UserWalletTransactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task TransferToBank_FromAWalletTheCallerDoesNotOwn_IsRejected()
    {
        using var context = CreateContext();
        var (_, other) = await SeedWalletsAsync(context);

        var provider = new StubBankService();
        var service = CreateService(context, provider);

        var (status, message, _) = await service.TransferToBank(TransferOf(other.Id), Query());

        status.Should().BeFalse();
        message.Should().Be("Source wallet not found");
        provider.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task TransferToBank_ReplayedWithTheSameClientReference_OnlyCallsTheProviderOnce()
    {
        using var context = CreateContext();
        var (source, _) = await SeedWalletsAsync(context);

        var provider = new StubBankService();
        var service = CreateService(context, provider);

        var request = TransferOf(source.Id);
        request.ClientReference = "payout-001";

        await service.TransferToBank(request, Query());
        var (status, message, data) = await service.TransferToBank(request, Query());

        status.Should().BeTrue();
        message.Should().Be("A transfer with this reference has already been submitted");
        data!.TransactionReference.Should().Be("payout-001");

        provider.CallCount.Should().Be(1);

        var wallet = await context.UserWallets.SingleAsync(x => x.Id == source.Id);
        wallet.WalletBalance.Should().Be(4500m);
    }
}
