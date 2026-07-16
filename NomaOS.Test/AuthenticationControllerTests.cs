using Agrovesto.Backend.API.Controllers.V2.Internal;
using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentAssertions;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;

namespace NomaOS.Test
{
    public class AuthenticationControllerTests
    {
        [Fact]
        public async Task VerifyOtp_ReturnsOk_WhenConfirmOtpSucceeds()
        {
            // Arrange
            var mockOnBoarding = new Mock<IOnBoardingManager>();
            mockOnBoarding.Setup(x => x.ConfirmOtp(It.IsAny<ConfirmOtpRequest>()))
                .ReturnsAsync(true);

            var httpContextAccessor = new HttpContextAccessor();
            var mockBusinessManager = new Mock<Agrovesto.Backend.Core.Interfaces.Managers.IBusinessManager>();

            var controller = new AuthenticationController(httpContextAccessor, mockOnBoarding.Object, mockBusinessManager.Object);

            var request = new ConfirmOtpRequest
            {
                UserId = "user-1",
                Code = "123456",
                Purpose = OtpPurpose.Onboarding
            };

            // Act
            var actionResult = await controller.VerifyOtp(request);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var ok = actionResult as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeOfType<Response<bool>>();
            var resp = ok.Value as Response<bool>;
            resp!.Succeeded.Should().BeTrue();
            resp.Data.Should().BeTrue();
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenCreateBusinessUserSucceeds()
        {
            // Arrange
            var mockOnBoarding = new Mock<IOnBoardingManager>();
            var responseModel = new OnboardingResponseModel
            {
                Id = "new-user-id",
                Message = "Created",
                IsExisting = false
            };

            mockOnBoarding.Setup(x => x.CreateBusinessUser(It.IsAny<OnboardingRequestModel>(), It.IsAny<Agrovesto.Backend.Core.V2.Onboarding.OnBoardingArgs>()))
                .ReturnsAsync(responseModel);

            var httpContextAccessor = new HttpContextAccessor();
            var mockBusinessManager = new Mock<Agrovesto.Backend.Core.Interfaces.Managers.IBusinessManager>();

            var controller = new AuthenticationController(httpContextAccessor, mockOnBoarding.Object, mockBusinessManager.Object);

            var requestModel = new OnboardingRequestModel
            {
                PhoneNumber = "07000000000",
                FullName = "Test User"
            };

            var args = new Agrovesto.Backend.Core.V2.Onboarding.OnBoardingArgs
            {
                userType = "Agent"
            };

            // Act
            var actionResult = await controller.Register(requestModel, args);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var ok = actionResult as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeOfType<Response<OnboardingResponseModel>>();
            var resp = ok.Value as Response<OnboardingResponseModel>;
            resp!.Succeeded.Should().BeTrue();
            resp.Data.Should().NotBeNull();
            resp.Data.Id.Should().Be(responseModel.Id);
        }
    }
}
