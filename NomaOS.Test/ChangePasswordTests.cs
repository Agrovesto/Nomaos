using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace NomaOS.Test;

public class ChangePasswordTests
{
    private const string Email = "agent@example.com";
    private const string CurrentPassword = "OldPass1";
    private const string NewPassword = "NewPass1";

    private static DataContext CreateContext() =>
        new(new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Mock<UserManager<User>> CreateUserManager() =>
        new(Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

    private static SignInManager<User> CreateSignInManager(UserManager<User> userManager)
    {
        var signInManager = new Mock<SignInManager<User>>(
            userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            null!, null!, null!, null!);

        signInManager.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

        return signInManager.Object;
    }

    private static OnboardingRepository CreateRepository(DataContext context, UserManager<User> userManager) =>
        new(context, CreateSignInManager(userManager), null!, null!, userManager,
            NullLogger<OnboardingRepository>.Instance);

    private static User BuildUser() => new()
    {
        Id = "AGENT-PWD-1",
        Email = Email,
        UserName = Email,
        UserType = "Agent",
        HasDefaultPassword = true
    };

    private static ChnagePasswordModel BuildModel() => new()
    {
        Email = Email,
        OldPassword = CurrentPassword,
        NewPassword = NewPassword,
        ConfirmPassword = NewPassword
    };

    [Fact]
    public async Task TheCurrentPasswordIsVerifiedBeforeTheChangeIsApplied()
    {
        using var context = CreateContext();
        var user = BuildUser();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);
        userManager.Setup(x => x.ChangePasswordAsync(user, CurrentPassword, NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var result = await CreateRepository(context, userManager.Object).ChangePassword(BuildModel());

        result.Status.Should().BeTrue();
        userManager.Verify(x => x.ChangePasswordAsync(user, CurrentPassword, NewPassword), Times.Once);
    }

    [Fact]
    public async Task AWrongCurrentPasswordThrowsInsteadOfReportingSuccess()
    {
        using var context = CreateContext();
        var user = BuildUser();

        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);
        userManager.Setup(x => x.ChangePasswordAsync(user, CurrentPassword, NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordMismatch",
                Description = "Incorrect password."
            }));

        var act = async () => await CreateRepository(context, userManager.Object).ChangePassword(BuildModel());

        var thrown = await act.Should().ThrowAsync<BaseException>();
        thrown.Which.ExceptionMessage.Should().Contain("Incorrect password.");
    }

    [Fact]
    public async Task AMismatchedConfirmPasswordIsRejectedBeforeIdentityIsCalled()
    {
        using var context = CreateContext();
        var user = BuildUser();

        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);

        var model = BuildModel();
        model.ConfirmPassword = "SomethingElse1";

        var act = async () => await CreateRepository(context, userManager.Object).ChangePassword(model);

        var thrown = await act.Should().ThrowAsync<BaseException>();
        thrown.Which.ExceptionMessage.Should().Contain("Confirm Password must match New Password");

        userManager.Verify(
            x => x.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ReusingTheCurrentPasswordIsRejected()
    {
        using var context = CreateContext();
        var user = BuildUser();

        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);

        var model = BuildModel();
        model.NewPassword = CurrentPassword;
        model.ConfirmPassword = CurrentPassword;

        var act = async () => await CreateRepository(context, userManager.Object).ChangePassword(model);

        var thrown = await act.Should().ThrowAsync<BaseException>();
        thrown.Which.ExceptionMessage.Should().Contain("different from your current password");
    }

    [Fact]
    public async Task AnUnknownEmailReportsInvalidEmailRatherThanCrashing()
    {
        using var context = CreateContext();

        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync((User)null!);

        var act = async () => await CreateRepository(context, userManager.Object).ChangePassword(BuildModel());

        var thrown = await act.Should().ThrowAsync<BaseException>();
        thrown.Which.ExceptionMessage.Should().Be("Invalid Email");
    }

    [Fact]
    public async Task AnAccountWithNoEmailIsToldToUpdateItsProfile()
    {
        using var context = CreateContext();

        var userManager = CreateUserManager();

        var model = BuildModel();
        model.Email = null!;

        var act = async () => await CreateRepository(context, userManager.Object).ChangePassword(model);

        var thrown = await act.Should().ThrowAsync<BaseException>();
        thrown.Which.ExceptionMessage.Should().Contain("no email address");

        userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AMissingCurrentPasswordIsRejectedBeforeIdentityIsCalled()
    {
        using var context = CreateContext();
        var user = BuildUser();

        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);

        var model = BuildModel();
        model.OldPassword = "";

        var act = async () => await CreateRepository(context, userManager.Object).ChangePassword(model);

        var thrown = await act.Should().ThrowAsync<BaseException>();
        thrown.Which.ExceptionMessage.Should().Contain("current password is required");

        userManager.Verify(
            x => x.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ASuccessfulChangeClearsTheDefaultPasswordFlag()
    {
        using var context = CreateContext();
        var user = BuildUser();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);
        userManager.Setup(x => x.ChangePasswordAsync(user, CurrentPassword, NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        await CreateRepository(context, userManager.Object).ChangePassword(BuildModel());

        var stored = await context.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id);
        stored.HasDefaultPassword.Should().BeFalse();
    }
}
