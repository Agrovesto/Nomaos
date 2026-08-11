using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace NomaOS.Test;

public class ValidateWalletPinTests
{
    private const string UserId = "user-1";
    private const string BusinessCode = "BIZ1";
    private const string CorrectPin = "1234";

    private static DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DataContext(options);
    }

    private static async Task<UserWallet> SeedWalletAsync(DataContext context, string pin = CorrectPin)
    {
        context.WalletProviders.Add(new WalletProvider
        {
            ProviderId = 1,
            Code = "MTN",
            Name = "MTN Momo",
            Status = "Active"
        });

        var wallet = new UserWallet
        {
            UserId = UserId,
            WalletProviderId = 1,
            WalletBalance = 1000,
            Currency = "NGN",
            IsActive = true,
            WalletNumber = "08000000000",
            BelongToBusinessCode = BusinessCode,
            IsDeleted = false,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        context.UserWallets.Add(wallet);
        await context.SaveChangesAsync();

        context.WalletSecurities.Add(new WalletSecurity
        {
            UserWalletId = wallet.Id,
            Txn = pin,
            TransactionPinHash = BCrypt.Net.BCrypt.HashPassword(pin)
        });
        await context.SaveChangesAsync();

        return wallet;
    }

    private static NomaOSWalletService CreateService(DataContext context)
    {
        var userWalletRepository = new UserWalletRepository(context, Mock.Of<IAWSService>());
        var otpService = new OtpService(context);

        return new NomaOSWalletService(
            Mock.Of<IBusinessRepository>(),
            Mock.Of<IOnboardingRepository>(),
            userWalletRepository,
            Mock.Of<IWalletProviderRepository>(),
            Mock.Of<IWalletServiceFactory>(),
            context,
            Mock.Of<IMTNMomo>(),
            Mock.Of<IInputFinancingRepository>(),
            Mock.Of<ILogger<NomaOSWalletService>>(),
            otpService);
    }

    private static WalletQueryParams QueryFor(long walletId) => new()
    {
        UserId = UserId,
        businessCode = BusinessCode,
        env = "prod",
        Id = walletId
    };

    [Fact]
    public async Task ValidateWalletPin_WithCorrectPin_ReturnsSuccessAndGeneratesActiveToken()
    {
        await using var context = CreateContext();
        var wallet = await SeedWalletAsync(context);
        var service = CreateService(context);

        var (status, message, token) = await service.ValidateWalletPin(
            new ValidateWalletPinRequest { WalletId = wallet.Id, Pin = CorrectPin },
            QueryFor(wallet.Id));

        status.Should().BeTrue();
        message.Should().Be("PIN is valid");
        token.Should().NotBeNull();

        // A short UUID: the 16 bytes of a GUID as URL-safe base64, padding trimmed.
        token!.Token.Should().MatchRegex("^[A-Za-z0-9_-]{22}$");
        token.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(3), TimeSpan.FromMinutes(1));

        var persisted = await context.OneTimePasswords.SingleAsync();
        persisted.UserId.Should().Be(UserId);
        persisted.Purpose.Should().Be(OtpPurpose.WalletPinUpdate);
        persisted.UsedAt.Should().BeNull();
        persisted.AttemptCount.Should().Be(0);

        // The token itself is never persisted - only its hash.
        persisted.CodeHash.Should().Be(OtpGenerator.Hash(token.Token));
        persisted.CodeHash.Should().NotBe(token.Token);
    }

    [Fact]
    public async Task ValidateWalletPin_WithIncorrectPin_ReturnsFailureAndNoTokenIsGenerated()
    {
        await using var context = CreateContext();
        var wallet = await SeedWalletAsync(context);
        var service = CreateService(context);

        var (status, message, token) = await service.ValidateWalletPin(
            new ValidateWalletPinRequest { WalletId = wallet.Id, Pin = "0000" },
            QueryFor(wallet.Id));

        status.Should().BeFalse();
        message.Should().Be("Invalid transaction PIN");
        token.Should().BeNull();
        (await context.OneTimePasswords.AnyAsync()).Should().BeFalse();

        var security = await context.WalletSecurities.SingleAsync(x => x.UserWalletId == wallet.Id);
        security.PinFailedAttempts.Should().Be(1);
    }

    [Fact]
    public async Task ValidateWalletPin_WithUnknownWallet_ThrowsNotFound()
    {
        await using var context = CreateContext();
        var wallet = await SeedWalletAsync(context);
        var service = CreateService(context);

        var act = () => service.ValidateWalletPin(
            new ValidateWalletPinRequest { WalletId = wallet.Id + 999, Pin = CorrectPin },
            QueryFor(wallet.Id + 999));

        var ex = await act.Should().ThrowAsync<BaseException>();
        ex.Which.ExceptionMessage.Should().Be("Wallet not found");
    }

    [Fact]
    public async Task ValidateWalletPin_WithEmptyPin_ThrowsBadRequest()
    {
        await using var context = CreateContext();
        var wallet = await SeedWalletAsync(context);
        var service = CreateService(context);

        var act = () => service.ValidateWalletPin(
            new ValidateWalletPinRequest { WalletId = wallet.Id, Pin = "" },
            QueryFor(wallet.Id));

        var ex = await act.Should().ThrowAsync<BaseException>();
        ex.Which.ExceptionMessage.Should().Be("Transaction PIN is required");
    }

    [Fact]
    public async Task GeneratedToken_IsValidImmediately_ThenExpires_AndStaysInactiveOnRetry()
    {
        await using var context = CreateContext();
        var otpService = new OtpService(context);

        var token = await otpService.GenerateTokenAsync(UserId, OtpPurpose.WalletPinUpdate);

        var (validNow, messageNow) = await otpService.ValidateTokenAsync(
            UserId, token.Token, OtpPurpose.WalletPinUpdate);
        validNow.Should().BeTrue();
        messageNow.Should().Be("Token is valid");

        // Validating does not consume the token - the follow-up update still needs it.
        var record = await context.OneTimePasswords.SingleAsync();
        record.UsedAt.Should().BeNull();

        // Simulate the three minute window having elapsed.
        record.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var (validAfterExpiry, messageAfterExpiry) = await otpService.ValidateTokenAsync(
            UserId, token.Token, OtpPurpose.WalletPinUpdate);
        validAfterExpiry.Should().BeFalse();
        messageAfterExpiry.Should().Be("Token has expired");

        record = await context.OneTimePasswords.SingleAsync();
        record.UsedAt.Should().NotBeNull();
        record.AttemptCount.Should().Be(2);

        // A further retry against the now-inactive token hits the "no longer active" branch.
        var (validOnRetry, messageOnRetry) = await otpService.ValidateTokenAsync(
            UserId, token.Token, OtpPurpose.WalletPinUpdate);
        validOnRetry.Should().BeFalse();
        messageOnRetry.Should().Be("Token is no longer active");
    }

    [Fact]
    public async Task ValidateToken_WithUnknownValue_ReturnsInvalid()
    {
        await using var context = CreateContext();
        var otpService = new OtpService(context);

        var (isValid, message) = await otpService.ValidateTokenAsync(
            UserId, "doesnotexist", OtpPurpose.WalletPinUpdate);

        isValid.Should().BeFalse();
        message.Should().Be("Invalid token");
    }

    [Fact]
    public async Task ValidateToken_ByAnotherUser_IsRejected()
    {
        await using var context = CreateContext();
        var otpService = new OtpService(context);

        var token = await otpService.GenerateTokenAsync(UserId, OtpPurpose.WalletPinUpdate);

        var (isValid, message) = await otpService.ValidateTokenAsync(
            "someone-else", token.Token, OtpPurpose.WalletPinUpdate);

        isValid.Should().BeFalse();
        message.Should().Be("Invalid token");

        // The rightful owner's token is untouched by the attempt.
        var record = await context.OneTimePasswords.SingleAsync();
        record.UsedAt.Should().BeNull();
        record.AttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateWalletPin_ConsumesTheToken_SoItCannotBeReplayed()
    {
        await using var context = CreateContext();
        var wallet = await SeedWalletAsync(context);
        var service = CreateService(context);

        var (_, _, token) = await service.ValidateWalletPin(
            new ValidateWalletPinRequest { WalletId = wallet.Id, Pin = CorrectPin },
            QueryFor(wallet.Id));

        var (status, message) = await service.UpdateWalletPin(
            new UpdateWalletPinRequest
            {
                WalletId = wallet.Id,
                NewPin = "5678",
                ConfirmNewPin = "5678",
                Token = token!.Token
            },
            QueryFor(wallet.Id));

        status.Should().BeTrue();
        message.Should().Be("Wallet PIN updated successfully");

        var record = await context.OneTimePasswords.SingleAsync();
        record.UsedAt.Should().NotBeNull();

        var (replayStatus, replayMessage) = await service.UpdateWalletPin(
            new UpdateWalletPinRequest
            {
                WalletId = wallet.Id,
                NewPin = "9999",
                ConfirmNewPin = "9999",
                Token = token.Token
            },
            QueryFor(wallet.Id));

        replayStatus.Should().BeFalse();
        replayMessage.Should().Be("Token is no longer active");
    }
}
