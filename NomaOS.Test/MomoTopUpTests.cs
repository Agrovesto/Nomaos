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

/// <summary>
/// Covers the MoMo top-up path: TopUpWallet writes a pending funding row, then hands off to
/// MTNMomo.InitiateWalletTopUpAsync, which is a RequestToPay. RequestToPay only ever returns
/// 202 Accepted - the money arrives later, via the callback or a status poll - so the states
/// this path leaves behind matter more than its return value.
/// </summary>
public class MomoTopUpTests
{
    private const string UserId = "user-1";
    private const string BusinessCode = "BIZ1";
    private const long ProviderId = 3;
    private const string Msisdn = "2349012981234";

    /// <summary>
    /// Stands in for MTNMomo. RequestToPay returns Accepted on the happy path, and MTNMomo
    /// rethrows on any exception rather than returning a Failed response, so the fake can do
    /// either.
    /// </summary>
    private sealed class FakeMomoService : IGenericWalletService
    {
        private readonly Func<WalletTopUpProviderRequest, WalletResponseService<PaymentResponse>> _topUp;

        public FakeMomoService(Func<WalletTopUpProviderRequest, WalletResponseService<PaymentResponse>> topUp)
            => _topUp = topUp;

        public WalletTopUpProviderRequest? LastRequest { get; private set; }

        public string ProviderKey => "Momo";
        public bool RequiresPreDebit => true;

        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTopUpAsync(
            WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string env)
        {
            LastRequest = request;
            return Task.FromResult(_topUp(request));
        }

        public Task<ProviderParams> RetrieveParams(WalletProviderParameterModel[] parameters, string env)
            => Task.FromResult(new ProviderParams());

        public Task<WalletResponseService<AccountValidationResponse>> CreateWalletAsync(
            UserProfileResponse data, WalletProviderParameterModel[] parameters, string env)
            => throw new NotSupportedException();

        public Task<WalletResponseService<AccountValidationResponse>> BalanceEnquiryAsync(
            UserProfileResponse data, WalletProviderParameterModel[] parameters, string env)
            => throw new NotSupportedException();

        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTransferAsync(
            TransferRequest request, WalletProviderParameterModel[] parameters, string env)
            => throw new NotSupportedException();

        public Task<WalletResponseService<PaymentStatusResponse>> TransactionStatusAsync(
            string reference, WalletProviderParameterModel[] parameters, string env)
            => throw new NotSupportedException();

        public Task<WalletResponseService<PaymentResponse>> DebitWalletAsync(
            WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string env)
            => throw new NotSupportedException();
    }

    private static DataContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            // HandleTopUpWebhook opens an explicit transaction, which the in-memory provider
            // cannot honour and warns about by default.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new DataContext(options);
    }

    private static async Task<UserWallet> SeedMomoWalletAsync(DataContext context, decimal balance = 0)
    {
        context.WalletProviders.Add(new WalletProvider
        {
            ProviderId = ProviderId,
            Code = "Momo",
            Name = "MTN Momo",
            Status = "Active"
        });

        var wallet = new UserWallet
        {
            UserId = UserId,
            WalletProviderId = ProviderId,
            WalletBalance = balance,
            Currency = "NGN",
            IsActive = true,
            WalletNumber = Msisdn,
            BelongToBusinessCode = BusinessCode,
            IsDeleted = false,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        context.UserWallets.Add(wallet);
        await context.SaveChangesAsync();

        return wallet;
    }

    private static NomaOSWalletService CreateService(DataContext context, IGenericWalletService momo)
    {
        var providerRepo = new Mock<IWalletProviderRepository>();

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
                new WalletProviderParameterModel { Key = "BaseUrl", Value = "https://sandbox.momodeveloper.mtn.com", ProviderId = ProviderId }
            });

        var factory = new Mock<IWalletServiceFactory>();
        factory.Setup(x => x.GetService("Momo")).Returns(momo);

        return new NomaOSWalletService(
            Mock.Of<IBusinessRepository>(),
            Mock.Of<IOnboardingRepository>(),
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

    private static TopUpWalletRequest TopUpOf(long walletId, decimal amount) => new()
    {
        UserWalletId = walletId,
        Amount = amount,
        WalletProviderCode = "Momo"
    };

    private static WalletQueryParams Query() => new()
    {
        UserId = UserId,
        businessCode = BusinessCode,
        env = "Demo"
    };

    /// <summary>
    /// RequestToPay answering 202 Accepted means the subscriber has been asked to approve on
    /// their handset, not that the money moved. The wallet must stay untouched until a
    /// callback or a status poll confirms it.
    /// </summary>
    [Fact]
    public async Task TopUpWallet_WhenRequestToPayIsAccepted_LeavesTheWalletUncreditedAndTheRowPending()
    {
        using var context = CreateContext();
        var wallet = await SeedMomoWalletAsync(context);

        var momo = new FakeMomoService(_ => WalletResponseService<PaymentResponse>.Success(
            new PaymentResponse { Status = "Accepted" }, "Dial *671*1*1*1*..."));

        var service = CreateService(context, momo);

        var response = await service.TopUpWallet(TopUpOf(wallet.Id, 500m), Query());

        response.IsSuccess.Should().BeTrue();

        var row = await context.UserWalletTransactions.SingleAsync();
        row.WalletTransactionStatus.Should().Be(WalletTransactionStatus.DebitPending);
        row.ProviderStatus.Should().Be("INITIATED");

        (await context.UserWallets.SingleAsync()).WalletBalance.Should().Be(0);
    }

    /// <summary>
    /// The pending row is written before the provider is called, and MTNMomo rethrows on any
    /// exception instead of returning a Failed response. TopUpWallet's outer catch then returns
    /// an error without calling CompleteWalletFunding, so the row is stranded in DebitPending.
    /// Nothing sweeps it: GetRequestToPayStatus has to be invoked with a reference, and a caller
    /// that saw only "Error topping up wallet" has no reason to hold on to one.
    /// </summary>
    [Fact]
    public async Task TopUpWallet_WhenTheProviderCallThrows_StrandsThePendingRow()
    {
        using var context = CreateContext();
        var wallet = await SeedMomoWalletAsync(context);

        var momo = new FakeMomoService(_ => throw new HttpRequestException("The request timed out"));

        var service = CreateService(context, momo);

        var response = await service.TopUpWallet(TopUpOf(wallet.Id, 500m), Query());

        response.IsSuccess.Should().BeFalse();
        response.Message.Should().Contain("Error topping up wallet");

        var row = await context.UserWalletTransactions.SingleAsync();
        row.WalletTransactionStatus.Should().Be(WalletTransactionStatus.DebitPending);
        row.ProviderStatus.Should().Be("INITIATED");

        // The reference that would let anyone reconcile this row is not handed back either.
        response.Data.Should().BeNull();
    }

    /// <summary>
    /// A declined RequestToPay does close the row out, so the failure path only diverges from
    /// the thrown path above.
    /// </summary>
    [Fact]
    public async Task TopUpWallet_WhenRequestToPayIsRejected_MarksTheRowFailed()
    {
        using var context = CreateContext();
        var wallet = await SeedMomoWalletAsync(context);

        var momo = new FakeMomoService(_ => WalletResponseService<PaymentResponse>.Failed(
            "Request failed with status code BadRequest (Bad Request)", 400));

        var service = CreateService(context, momo);

        var response = await service.TopUpWallet(TopUpOf(wallet.Id, 500m), Query());

        response.IsSuccess.Should().BeFalse();

        var row = await context.UserWalletTransactions.SingleAsync();
        row.WalletTransactionStatus.Should().Be(WalletTransactionStatus.Failed);
    }

    /// <summary>
    /// TopUpWallet always sends the wallet's own currency. MTNMomo then overwrites it with EUR
    /// whenever env is Demo, so in the sandbox the row says NGN 500 while MTN is asked for
    /// EUR 500. Amount and currency are recorded before the provider can restate either.
    /// </summary>
    [Fact]
    public async Task TopUpWallet_SendsTheWalletCurrency_WhichMomoOverridesInDemo()
    {
        using var context = CreateContext();
        var wallet = await SeedMomoWalletAsync(context);

        var momo = new FakeMomoService(_ => WalletResponseService<PaymentResponse>.Success(
            new PaymentResponse { Status = "Accepted" }));

        var service = CreateService(context, momo);

        await service.TopUpWallet(TopUpOf(wallet.Id, 500m), Query());

        momo.LastRequest!.Currency.Should().Be("NGN");
        momo.LastRequest.WalletNumber.Should().Be(Msisdn);

        var row = await context.UserWalletTransactions.SingleAsync();
        row.Currency.Should().Be("NGN");
        row.TransactionReference.Should().Be(momo.LastRequest.TransactionReference);
    }

    /// <summary>
    /// Two top-ups in a row mint two references and two pending rows. MoMo dedupes on
    /// X-Reference-Id, so a client that retries because it never saw the first response gets a
    /// second live RequestToPay rather than the 409 that a stable reference would earn.
    /// </summary>
    [Fact]
    public async Task TopUpWallet_CalledTwice_CreatesTwoIndependentPendingRows()
    {
        using var context = CreateContext();
        var wallet = await SeedMomoWalletAsync(context);

        var momo = new FakeMomoService(_ => WalletResponseService<PaymentResponse>.Success(
            new PaymentResponse { Status = "Accepted" }));

        var service = CreateService(context, momo);

        await service.TopUpWallet(TopUpOf(wallet.Id, 500m), Query());
        await service.TopUpWallet(TopUpOf(wallet.Id, 500m), Query());

        var rows = await context.UserWalletTransactions.ToListAsync();
        rows.Should().HaveCount(2);
        rows.Select(x => x.TransactionReference).Distinct().Should().HaveCount(2);
        rows.Should().OnlyContain(x => x.WalletTransactionStatus == WalletTransactionStatus.DebitPending);
    }

    /// <summary>
    /// complete-payment polls MTN and funnels the result into HandleTopUpWebhook. Every write on
    /// that path went through repository helpers whose SaveChangesAsync was commented out, and
    /// the method committed a transaction that had issued no SQL, so a successful collection was
    /// reported as "Successful" while the balance never moved. Asserted through a second context
    /// so tracked-but-unsaved state cannot pass for a write.
    /// </summary>
    [Fact]
    public async Task HandleTopUpWebhook_OnSuccess_PersistsTheCreditAndCompletesTheRow()
    {
        var databaseName = Guid.NewGuid().ToString();

        string reference;
        long walletId;

        using (var context = CreateContext(databaseName))
        {
            var wallet = await SeedMomoWalletAsync(context, balance: 1000m);
            walletId = wallet.Id;

            var momo = new FakeMomoService(_ => WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Accepted" }));

            var service = CreateService(context, momo);

            await service.TopUpWallet(TopUpOf(wallet.Id, 500m), Query());
            reference = momo.LastRequest!.TransactionReference;

            await service.HandleTopUpWebhook(new ProviderWebhookRequest
            {
                TransactionReference = reference,
                ProviderReference = "momo-fin-txn-1",
                Status = "SUCCESSFUL",
                Provider = "Momo"
            });
        }

        using var verification = CreateContext(databaseName);

        var creditedWallet = await verification.UserWallets.SingleAsync(x => x.Id == walletId);
        creditedWallet.WalletBalance.Should().Be(1500m);

        var row = await verification.UserWalletTransactions
            .SingleAsync(x => x.TransactionReference == reference);

        row.WalletTransactionStatus.Should().Be(WalletTransactionStatus.Completed);
        row.BalanceBefore.Should().Be(1000m);
        row.BalanceAfter.Should().Be(1500m);
        row.ProviderReference.Should().Be("momo-fin-txn-1");
    }

    /// <summary>
    /// The MTN callback never carries a business code, and the old lookup filtered the wallet on
    /// it, silently skipping the credit. The wallet is taken from the transaction now, so an
    /// absent business code must still credit.
    /// </summary>
    [Fact]
    public async Task HandleTopUpWebhook_WithNoBusinessCode_StillCreditsTheWallet()
    {
        var databaseName = Guid.NewGuid().ToString();

        string reference;
        long walletId;

        using (var context = CreateContext(databaseName))
        {
            var wallet = await SeedMomoWalletAsync(context, balance: 250m);
            walletId = wallet.Id;

            var momo = new FakeMomoService(_ => WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Accepted" }));

            var service = CreateService(context, momo);

            await service.TopUpWallet(TopUpOf(wallet.Id, 100m), Query());
            reference = momo.LastRequest!.TransactionReference;

            await service.HandleTopUpWebhook(new ProviderWebhookRequest
            {
                TransactionReference = reference,
                Status = "SUCCESSFUL",
                BusinessCode = null
            });
        }

        using var verification = CreateContext(databaseName);

        (await verification.UserWallets.SingleAsync(x => x.Id == walletId))
            .WalletBalance.Should().Be(350m);
    }

    /// <summary>
    /// A callback that arrives twice - or a callback racing the complete-payment poll - must not
    /// credit twice.
    /// </summary>
    [Fact]
    public async Task HandleTopUpWebhook_DeliveredTwice_CreditsOnlyOnce()
    {
        var databaseName = Guid.NewGuid().ToString();

        string reference;
        long walletId;

        using (var context = CreateContext(databaseName))
        {
            var wallet = await SeedMomoWalletAsync(context, balance: 1000m);
            walletId = wallet.Id;

            var momo = new FakeMomoService(_ => WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Accepted" }));

            var service = CreateService(context, momo);

            await service.TopUpWallet(TopUpOf(wallet.Id, 500m), Query());
            reference = momo.LastRequest!.TransactionReference;

            var webhook = new ProviderWebhookRequest
            {
                TransactionReference = reference,
                Status = "SUCCESSFUL"
            };

            await service.HandleTopUpWebhook(webhook);

            var second = await service.HandleTopUpWebhook(webhook);
            second.Should().Be("Transaction Completed or Reversed");
        }

        using var verification = CreateContext(databaseName);

        (await verification.UserWallets.SingleAsync(x => x.Id == walletId))
            .WalletBalance.Should().Be(1500m);
    }

    /// <summary>
    /// A failed collection must persist as Failed rather than being left pending - the status
    /// update was on the same unsaved path as the credit.
    /// </summary>
    [Fact]
    public async Task HandleTopUpWebhook_OnFailure_PersistsTheFailedStatusAndLeavesTheBalance()
    {
        var databaseName = Guid.NewGuid().ToString();

        string reference;
        long walletId;

        using (var context = CreateContext(databaseName))
        {
            var wallet = await SeedMomoWalletAsync(context, balance: 1000m);
            walletId = wallet.Id;

            var momo = new FakeMomoService(_ => WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Accepted" }));

            var service = CreateService(context, momo);

            await service.TopUpWallet(TopUpOf(wallet.Id, 500m), Query());
            reference = momo.LastRequest!.TransactionReference;

            await service.HandleTopUpWebhook(new ProviderWebhookRequest
            {
                TransactionReference = reference,
                Status = "FAILED"
            });
        }

        using var verification = CreateContext(databaseName);

        (await verification.UserWallets.SingleAsync(x => x.Id == walletId))
            .WalletBalance.Should().Be(1000m);

        (await verification.UserWalletTransactions.SingleAsync(x => x.TransactionReference == reference))
            .WalletTransactionStatus.Should().Be(WalletTransactionStatus.Failed);
    }

    /// <summary>
    /// MTNMomo serialises the amount as request.Amount.ToString("0"), which rounds. The row
    /// keeps the exact decimal, so any amount with kobo asks MTN for a different number than
    /// the one that will be credited on the callback.
    /// </summary>
    [Theory]
    [InlineData("600.75", "601")]
    [InlineData("600.40", "600")]
    [InlineData("0.90", "1")]
    public void MomoAmountFormat_RoundsAwayTheKobo(string amount, string expectedPayloadValue)
    {
        decimal.Parse(amount).ToString("0").Should().Be(expectedPayloadValue);
    }
}
