using System.Text.RegularExpressions;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace NomaOS.Test;

public class WalletPinResetOtpTests
{
    private const string UserId = "user-1";
    private const string BusinessCode = "BIZ1";
    private const string SenderEmail = "no-reply@nomaos.test";

    private static DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DataContext(options);
    }

    private static async Task<UserWallet> SeedWalletAsync(DataContext context, string pin)
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

    private static Mock<IAWSService> CapturingMailer(ICollection<string> bodies)
    {
        var awsService = new Mock<IAWSService>();
        awsService
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string, string, string>((_, _, _, bodyHtml, _) => bodies.Add(bodyHtml))
            .Returns(Task.CompletedTask);

        return awsService;
    }

    private static string CodeFrom(string emailBody) => Regex.Match(emailBody, @"\b\d{6}\b").Value;

    private static NomaOSWalletService CreateService(
        DataContext context,
        Mock<IAWSService> awsService,
        string? userEmail,
        bool userFound = true)
    {
        var onboardingRepository = new Mock<IOnboardingRepository>();
        onboardingRepository
            .Setup(x => x.GetUserById(UserId))
            .ReturnsAsync((
                userFound ? new UserModel { Id = UserId, Email = userEmail } : null!,
                userFound,
                userFound ? string.Empty : "User not found"));

        var configuration = new Mock<IConfiguration>();
        configuration.Setup(x => x["Official:Email"]).Returns(SenderEmail);

        return new NomaOSWalletService(
            Mock.Of<IBusinessRepository>(),
            onboardingRepository.Object,
            new UserWalletRepository(context, Mock.Of<IAWSService>()),
            Mock.Of<IWalletProviderRepository>(),
            Mock.Of<IWalletServiceFactory>(),
            context,
            Mock.Of<IMTNMomo>(),
            Mock.Of<IInputFinancingRepository>(),
            Mock.Of<ILogger<NomaOSWalletService>>(),
            new OtpService(context),
            awsService.Object,
            configuration.Object);
    }

    private static WalletQueryParams Query() => new()
    {
        UserId = UserId,
        businessCode = BusinessCode,
        env = "prod"
    };

    [Fact]
    public async Task SendWalletPinResetOtp_EmailsTheCode_AndStoresOnlyItsHash()
    {
        await using var context = CreateContext();

        string? sentBody = null;
        var awsService = new Mock<IAWSService>();
        awsService
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string, string, string>((_, _, _, bodyHtml, _) => sentBody = bodyHtml)
            .Returns(Task.CompletedTask);

        var service = CreateService(context, awsService, "farmer@example.com");

        var (status, message, data) = await service.SendWalletPinResetOtp(Query());

        status.Should().BeTrue();
        message.Should().Be("A reset code has been sent to your email address");

        // The address is masked on the way out.
        data.Should().NotBeNull();
        data!.Destination.Should().Be("f*****@example.com");
        data.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromMinutes(1));

        awsService.Verify(x => x.SendEmailAsync(
            SenderEmail,
            "farmer@example.com",
            "Reset your wallet PIN",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);

        // The code that landed in the inbox is the one that was stored - as a hash, never in the clear.
        var emailedCode = Regex.Match(sentBody!, @"\b\d{6}\b").Value;
        emailedCode.Should().NotBeEmpty();

        var otp = await context.OneTimePasswords.SingleAsync();
        otp.UserId.Should().Be(UserId);
        otp.Purpose.Should().Be(OtpPurpose.WalletPinReset);
        otp.Target.Should().Be(OtpTarget.Email);
        otp.Destination.Should().Be("farmer@example.com");
        otp.UsedAt.Should().BeNull();
        otp.CodeHash.Should().Be(OtpGenerator.Hash(emailedCode));
        otp.CodeHash.Should().NotBe(emailedCode);
    }

    [Fact]
    public async Task SendWalletPinResetOtp_WithNoEmailOnFile_ReturnsFailureAndSendsNothing()
    {
        await using var context = CreateContext();
        var awsService = new Mock<IAWSService>();

        var service = CreateService(context, awsService, userEmail: null);

        var (status, message, data) = await service.SendWalletPinResetOtp(Query());

        status.Should().BeFalse();
        message.Should().Be("No email address is on file for this account");
        data.Should().BeNull();

        (await context.OneTimePasswords.AnyAsync()).Should().BeFalse();
        awsService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendWalletPinResetOtp_WithUnknownUser_ThrowsNotFound()
    {
        await using var context = CreateContext();
        var awsService = new Mock<IAWSService>();

        var service = CreateService(context, awsService, userEmail: null, userFound: false);

        var act = () => service.SendWalletPinResetOtp(Query());

        var ex = await act.Should().ThrowAsync<BaseException>();
        ex.Which.ExceptionMessage.Should().Be("User not found");
        awsService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendWalletPinResetOtp_IsRateLimitedAfterThreeRequests()
    {
        await using var context = CreateContext();
        var awsService = new Mock<IAWSService>();
        awsService
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(context, awsService, "farmer@example.com");

        for (var i = 0; i < 3; i++)
            (await service.SendWalletPinResetOtp(Query())).status.Should().BeTrue();

        var act = () => service.SendWalletPinResetOtp(Query());

        var ex = await act.Should().ThrowAsync<BaseException>();
        ex.Which.ExceptionMessage.Should().Be("Too many OTP requests. Please try again later.");

        // The fourth request never reached the mail service.
        awsService.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task SendWalletPinResetOtp_SupersedesThePreviousCode()
    {
        await using var context = CreateContext();

        var bodies = new List<string>();
        var awsService = new Mock<IAWSService>();
        awsService
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string, string, string>((_, _, _, bodyHtml, _) => bodies.Add(bodyHtml))
            .Returns(Task.CompletedTask);

        var service = CreateService(context, awsService, "farmer@example.com");

        await service.SendWalletPinResetOtp(Query());
        await service.SendWalletPinResetOtp(Query());

        var firstCode = Regex.Match(bodies[0], @"\b\d{6}\b").Value;
        var secondCode = Regex.Match(bodies[1], @"\b\d{6}\b").Value;

        var otpService = new OtpService(context);

        // Only the newest code still verifies.
        (await otpService.VerifyOtpAsync(UserId, firstCode, OtpPurpose.WalletPinReset)).Should().BeNull();
        (await otpService.VerifyOtpAsync(UserId, secondCode, OtpPurpose.WalletPinReset)).Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyWalletPinResetOtp_WithTheEmailedCode_ConsumesItAndIssuesAToken()
    {
        await using var context = CreateContext();
        var bodies = new List<string>();
        var awsService = CapturingMailer(bodies);
        var service = CreateService(context, awsService, "farmer@example.com");

        await service.SendWalletPinResetOtp(Query());

        var (status, message, token) = await service.VerifyWalletPinResetOtp(
            new VerifyPinResetOtpRequest { Code = CodeFrom(bodies.Single()) }, Query());

        status.Should().BeTrue();
        message.Should().Be("Reset code is valid");
        token.Should().NotBeNull();
        token!.Token.Should().MatchRegex("^[A-Za-z0-9_-]{22}$");
        token.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(3), TimeSpan.FromMinutes(1));

        // The emailed code is spent, and a separate token row now backs the returned token.
        var otp = await context.OneTimePasswords.SingleAsync(x => x.Purpose == OtpPurpose.WalletPinReset);
        otp.UsedAt.Should().NotBeNull();

        var tokenRow = await context.OneTimePasswords.SingleAsync(x => x.Purpose == OtpPurpose.WalletPinUpdate);
        tokenRow.UsedAt.Should().BeNull();
        tokenRow.CodeHash.Should().Be(OtpGenerator.Hash(token.Token));
    }

    [Fact]
    public async Task VerifyWalletPinResetOtp_WithWrongCode_IssuesNoToken()
    {
        await using var context = CreateContext();
        var bodies = new List<string>();
        var awsService = CapturingMailer(bodies);
        var service = CreateService(context, awsService, "farmer@example.com");

        await service.SendWalletPinResetOtp(Query());

        var (status, message, token) = await service.VerifyWalletPinResetOtp(
            new VerifyPinResetOtpRequest { Code = "000000" }, Query());

        status.Should().BeFalse();
        message.Should().Be("The reset code is invalid or has expired");
        token.Should().BeNull();

        (await context.OneTimePasswords.AnyAsync(x => x.Purpose == OtpPurpose.WalletPinUpdate)).Should().BeFalse();

        // The real code survives the wrong guess, with the attempt counted against it.
        var otp = await context.OneTimePasswords.SingleAsync(x => x.Purpose == OtpPurpose.WalletPinReset);
        otp.UsedAt.Should().BeNull();
        otp.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task VerifyWalletPinResetOtp_WithTheSameCodeTwice_IsRejectedTheSecondTime()
    {
        await using var context = CreateContext();
        var bodies = new List<string>();
        var awsService = CapturingMailer(bodies);
        var service = CreateService(context, awsService, "farmer@example.com");

        await service.SendWalletPinResetOtp(Query());
        var code = CodeFrom(bodies.Single());

        (await service.VerifyWalletPinResetOtp(new VerifyPinResetOtpRequest { Code = code }, Query()))
            .status.Should().BeTrue();

        var (status, message, token) = await service.VerifyWalletPinResetOtp(
            new VerifyPinResetOtpRequest { Code = code }, Query());

        status.Should().BeFalse();
        message.Should().Be("The reset code is invalid or has expired");
        token.Should().BeNull();
    }

    [Fact]
    public async Task ForgotPin_EndToEnd_EmailedCodeBecomesANewPin()
    {
        await using var context = CreateContext();
        var wallet = await SeedWalletAsync(context, "1234");
        var bodies = new List<string>();
        var awsService = CapturingMailer(bodies);
        var service = CreateService(context, awsService, "farmer@example.com");

        // 1. The user has forgotten their PIN, so they ask for a code.
        (await service.SendWalletPinResetOtp(Query())).status.Should().BeTrue();

        // 2. They enter the code from their inbox and get a token back.
        var (verified, _, token) = await service.VerifyWalletPinResetOtp(
            new VerifyPinResetOtpRequest { Code = CodeFrom(bodies.Single()) }, Query());
        verified.Should().BeTrue();

        // 3. They spend that token on update-pin, which never knew a PIN was not entered.
        var query = Query();
        query.Id = wallet.Id;

        var (updated, updateMessage) = await service.UpdateWalletPin(
            new UpdateWalletPinRequest
            {
                WalletId = wallet.Id,
                NewPin = "5678",
                ConfirmNewPin = "5678",
                Token = token!.Token
            },
            query);

        updated.Should().BeTrue();
        updateMessage.Should().Be("Wallet PIN updated successfully");

        var security = await context.WalletSecurities.SingleAsync(x => x.UserWalletId == wallet.Id);
        BCrypt.Net.BCrypt.Verify("5678", security.TransactionPinHash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("1234", security.TransactionPinHash).Should().BeFalse();
    }
}
