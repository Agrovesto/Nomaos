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

public class WalletWithdrawalTests
{
    private const string UserId = "user-1";
    private const string BusinessCode = "BIZ1";
    private const long ProviderId = 3;
    private const string WalletNumber = "2349012981234";
    private const string Pin = "1234";

    private sealed class CapturingTransferService : IGenericWalletService
    {
        private readonly Func<TransferRequest, WalletResponseService<PaymentResponse>>? _transfer;

        public CapturingTransferService(Func<TransferRequest, WalletResponseService<PaymentResponse>>? transfer = null)
            => _transfer = transfer;

        public TransferRequest? LastRequest { get; private set; }

        public string ProviderKey => "Momo";
        public bool RequiresPreDebit => true;

        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTransferAsync(
            TransferRequest request, WalletProviderParameterModel[] parameters, string env)
        {
            LastRequest = request;

            if (_transfer != null)
                return Task.FromResult(_transfer(request));

            return Task.FromResult(WalletResponseService<PaymentResponse>.Success(
                new PaymentResponse { Status = "Successful", Reference = request.Reference }, "Transfer completed"));
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

        public Task<WalletResponseService<PaymentResponse>> DebitWalletAsync(
            WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string env)
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

    private static async Task<UserWallet> SeedWalletAsync(DataContext context, decimal balance = 5000m)
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
            WalletNumber = WalletNumber,
            BelongToBusinessCode = BusinessCode,
            IsDeleted = false,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        context.UserWallets.Add(wallet);
        await context.SaveChangesAsync();

        context.WalletSecurities.Add(new WalletSecurity
        {
            UserWalletId = wallet.Id,
            Txn = Pin,
            TransactionPinHash = BCrypt.Net.BCrypt.HashPassword(Pin)
        });
        await context.SaveChangesAsync();

        return wallet;
    }

    private static NomaOSWalletService CreateService(
        DataContext context,
        IGenericWalletService provider,
        UserModel? user)
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
                new WalletProviderParameterModel { Key = "BaseUrl", Value = "https://example.test", ProviderId = ProviderId }
            });

        var factory = new Mock<IWalletServiceFactory>();
        factory.Setup(x => x.GetService("Momo")).Returns(provider);

        var onboardingRepository = new Mock<IOnboardingRepository>();
        onboardingRepository
            .Setup(x => x.GetUserById(UserId))
            .ReturnsAsync((user!, user != null, user != null ? string.Empty : "No record found"));

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
        UserId = UserId,
        businessCode = BusinessCode,
        env = "Demo"
    };

    private static WithdrawWalletRequest WithdrawalOf(long walletId, string? recipientName = null) => new()
    {
        UserWalletId = walletId,
        Amount = 500m,
        WalletProviderCode = "Momo",
        TransactionPin = Pin,
        RecipientName = recipientName
    };

    [Fact]
    public async Task WithdrawWallet_SendsTheSenderAndRecipientTheProviderRequires()
    {
        using var context = CreateContext();
        var wallet = await SeedWalletAsync(context);

        var provider = new CapturingTransferService();
        var service = CreateService(context, provider, new UserModel { Id = UserId, Name = "Ada Lovelace" });

        var response = await service.WithdrawWallet(WithdrawalOf(wallet.Id), Query());

        response.IsSuccess.Should().BeTrue();

        provider.LastRequest.Should().NotBeNull();
        provider.LastRequest!.SenderName.Should().Be("Ada Lovelace");
        provider.LastRequest.SenderAccountNumber.Should().Be(WalletNumber);
        provider.LastRequest.RecipientName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task WithdrawWallet_FallsBackToFirstAndLastNameWhenNameIsBlank()
    {
        using var context = CreateContext();
        var wallet = await SeedWalletAsync(context);

        var provider = new CapturingTransferService();
        var service = CreateService(context, provider,
            new UserModel { Id = UserId, FirstName = "Grace", LastName = "Hopper" });

        await service.WithdrawWallet(WithdrawalOf(wallet.Id), Query());

        provider.LastRequest!.SenderName.Should().Be("Grace Hopper");
    }

    [Fact]
    public async Task WithdrawWallet_KeepsTheSuppliedRecipientName()
    {
        using var context = CreateContext();
        var wallet = await SeedWalletAsync(context);

        var provider = new CapturingTransferService();
        var service = CreateService(context, provider, new UserModel { Id = UserId, Name = "Ada Lovelace" });

        await service.WithdrawWallet(WithdrawalOf(wallet.Id, recipientName: "Charles Babbage"), Query());

        provider.LastRequest!.SenderName.Should().Be("Ada Lovelace");
        provider.LastRequest.RecipientName.Should().Be("Charles Babbage");
    }

    [Fact]
    public async Task WithdrawWallet_WithNoNameOnFile_FailsWithoutCallingTheProvider()
    {
        using var context = CreateContext();
        var wallet = await SeedWalletAsync(context);

        var provider = new CapturingTransferService();
        var service = CreateService(context, provider, new UserModel { Id = UserId });

        var response = await service.WithdrawWallet(WithdrawalOf(wallet.Id), Query());

        response.IsSuccess.Should().BeFalse();
        response.Message.Should().Contain("No account name is on file");

        provider.LastRequest.Should().BeNull();
        (await context.UserWalletTransactions.AnyAsync()).Should().BeFalse();
        (await context.UserWallets.SingleAsync()).WalletBalance.Should().Be(5000m);
    }
}
