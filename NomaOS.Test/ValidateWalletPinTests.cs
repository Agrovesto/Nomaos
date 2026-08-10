using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Logic.Services;
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
        var tokenService = new TokenService(context);

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
            tokenService);
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
        token.Should().NotBeNullOrEmpty();
        token!.Length.Should().Be(9);
        token.Should().MatchRegex("^[0-9a-f]{9}$");

        var persisted = await context.Tokens.SingleAsync(x => x.Value == token);
        persisted.OwnerId.Should().Be(UserId);
        persisted.Use.Should().Be(TokenUse.WalletPinUpdate);
        persisted.IsActive.Should().BeTrue();
        persisted.LastAttemptOn.Should().BeNull();
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
        (await context.Tokens.AnyAsync()).Should().BeFalse();

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
    public async Task GeneratedToken_IsValidImmediately_ThenExpiresAfterThreeMinutes_AndStaysInactiveOnRetry()
    {
        await using var context = CreateContext();
        var tokenService = new TokenService(context);

        var token = await tokenService.GenerateToken(UserId, TokenUse.WalletPinUpdate);

        var (validNow, messageNow) = await tokenService.ValidateToken(token);
        validNow.Should().BeTrue();
        messageNow.Should().Be("Token is valid");

        // Simulate 4 minutes having elapsed since generation.
        var record = await context.Tokens.SingleAsync(x => x.Value == token);
        record.GeneratedOn = DateTimeOffset.UtcNow.AddMinutes(-4);
        await context.SaveChangesAsync();

        var (validAfterExpiry, messageAfterExpiry) = await tokenService.ValidateToken(token);
        validAfterExpiry.Should().BeFalse();
        messageAfterExpiry.Should().Be("Token has expired");

        record = await context.Tokens.SingleAsync(x => x.Value == token);
        record.IsActive.Should().BeFalse();
        record.LastAttemptOn.Should().NotBeNull();

        // A further retry against the now-inactive token hits the "no longer active" branch.
        var (validOnRetry, messageOnRetry) = await tokenService.ValidateToken(token);
        validOnRetry.Should().BeFalse();
        messageOnRetry.Should().Be("Token is no longer active");
    }

    [Fact]
    public async Task ValidateToken_WithUnknownValue_ReturnsInvalid()
    {
        await using var context = CreateContext();
        var tokenService = new TokenService(context);

        var (isValid, message) = await tokenService.ValidateToken("doesnotexist");

        isValid.Should().BeFalse();
        message.Should().Be("Invalid token");
    }
}
