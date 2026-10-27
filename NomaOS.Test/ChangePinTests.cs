using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace NomaOS.Test;

public class ChangePinTests
{
    private const string UserId = "AGENT-PIN-1";
    private const string CurrentPin = "1234";

    private static DataContext CreateContext() =>
        new(new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SignInManager<User> CreateSignInManager()
    {
        var userManager = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        var signInManager = new Mock<SignInManager<User>>(
            userManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            null!, null!, null!, null!);

        signInManager.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

        return signInManager.Object;
    }

    private static OnboardingRepository CreateRepository(DataContext context) =>
        new(context, CreateSignInManager(), null!, null!, null!, NullLogger<OnboardingRepository>.Instance, null!, null!);

    private static async Task<User> SeedUserAsync(DataContext context, string pin = CurrentPin)
    {
        var salt = EncryptionUtil.GetSalt();

        var user = new User
        {
            Id = UserId,
            UserName = "08012345678",
            PhoneNumber = "08012345678",
            UserType = "Agent",
            Pin = pin,
            Salt = Convert.ToBase64String(salt),
            PinEncrypt = pin.EncryptTransactionPin(salt),
            HasDefaultPin = true
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task TheStoredPinIsAcceptedByValidation()
    {
        using var context = CreateContext();
        await SeedUserAsync(context);

        var valid = await CreateRepository(context).ValidateUserPinAsync(UserId, CurrentPin);

        valid.Should().BeTrue();
    }

    [Fact]
    public async Task AWrongPinIsRejectedByValidation()
    {
        using var context = CreateContext();
        await SeedUserAsync(context);

        var valid = await CreateRepository(context).ValidateUserPinAsync(UserId, "9999");

        valid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidationIsRejectedForAnUnknownUser()
    {
        using var context = CreateContext();
        await SeedUserAsync(context);

        var valid = await CreateRepository(context).ValidateUserPinAsync("NOBODY", CurrentPin);

        valid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidationIsRejectedWhenTheAccountHasNoPinStored()
    {
        using var context = CreateContext();
        await context.Users.AddAsync(new User
        {
            Id = UserId,
            UserName = "08012345678",
            PhoneNumber = "08012345678",
            UserType = "Agent"
        });
        await context.SaveChangesAsync();

        var valid = await CreateRepository(context).ValidateUserPinAsync(UserId, CurrentPin);

        valid.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPinStoresTheNewPinTheSameWaySignupDoes()
    {
        using var context = CreateContext();
        var user = await SeedUserAsync(context);
        var repository = CreateRepository(context);

        await repository.ResetPin(user, "5678");

        var stored = await context.Users.AsNoTracking().SingleAsync(x => x.Id == UserId);

        var salt = Convert.FromBase64String(stored.Salt);
        stored.PinEncrypt.Should().Be("5678".EncryptTransactionPin(salt));
        stored.HasDefaultPin.Should().BeFalse();
    }

    [Fact]
    public async Task TheNewPinValidatesAfterAChangeAndTheOldOneNoLongerDoes()
    {
        using var context = CreateContext();
        var user = await SeedUserAsync(context);
        var repository = CreateRepository(context);

        await repository.ResetPin(user, "5678");

        (await repository.ValidateUserPinAsync(UserId, "5678")).Should().BeTrue();
        (await repository.ValidateUserPinAsync(UserId, CurrentPin)).Should().BeFalse();
    }

    [Fact]
    public async Task EachChangeRotatesTheSaltSoTheStoredHashDiffers()
    {
        using var context = CreateContext();
        var user = await SeedUserAsync(context);
        var repository = CreateRepository(context);

        var originalSalt = user.Salt;
        var originalHash = user.PinEncrypt;

        await repository.ResetPin(user, CurrentPin);

        var stored = await context.Users.AsNoTracking().SingleAsync(x => x.Id == UserId);

        stored.Salt.Should().NotBe(originalSalt);
        stored.PinEncrypt.Should().NotBe(originalHash);
        (await repository.ValidateUserPinAsync(UserId, CurrentPin)).Should().BeTrue();
    }

    [Fact]
    public void ConfirmPinMismatchIsRejectedByTheModelContract()
    {
        var model = new ChangePinModel
        {
            OldPin = CurrentPin,
            NewPin = "5678",
            ConfirmPin = "8765"
        };

        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var valid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            model,
            new System.ComponentModel.DataAnnotations.ValidationContext(model),
            results,
            validateAllProperties: true);

        valid.Should().BeFalse();
        results.Should().Contain(r => r.ErrorMessage!.Contains("Confirm PIN must match New PIN"));
    }

    [Fact]
    public void MatchingConfirmPinPassesTheModelContract()
    {
        var model = new ChangePinModel
        {
            OldPin = CurrentPin,
            NewPin = "5678",
            ConfirmPin = "5678"
        };

        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var valid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            model,
            new System.ComponentModel.DataAnnotations.ValidationContext(model),
            results,
            validateAllProperties: true);

        valid.Should().BeTrue();
    }
}
