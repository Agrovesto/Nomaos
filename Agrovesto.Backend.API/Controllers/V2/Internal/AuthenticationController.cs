using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/account")]
    [ApiController]
    public class AuthenticationController : BaseController
    {
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly IBusinessManager _businessManager;
        
        private readonly IBulkFarmerUploadService _bulkFarmerUploadService;
        private readonly ILogger<AuthenticationController> _logger;
        public record LogoutRequest(string RefreshToken);
        public AuthenticationController(IHttpContextAccessor httpContextAccessor,
            IOnBoardingManager onBoardingManager,
            IBusinessManager businessManager, IBulkFarmerUploadService bulkFarmerUploadService, 
            ILogger<AuthenticationController> logger) : base(httpContextAccessor)
        {
            _onBoardingManager = onBoardingManager;
            _businessManager = businessManager;
            _bulkFarmerUploadService = bulkFarmerUploadService;
            _logger = logger;
        }

        [HttpPost("bulk-onboard")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> BulkOnboard(IFormFile file, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            args.OnboardedByUserId = CurrentUserId;
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;

            if (CurrentUserId != null && CurrentUserRole != args.userType)
                await EnrichArgsByRoleAsync(args);

            return await HandleBulkFarmerUploadAsync(file, args, _bulkFarmerUploadService, _logger);
        }
        [HttpPost("create-account")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] OnboardingRequestModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(async () =>
            {
                args.OnboardedByUserId = CurrentUserId;
             

                if (CurrentUserId != null && CurrentUserRole != args.userType)
                    await EnrichArgsByRoleAsync(args);

                return await _onBoardingManager.CreateBusinessUser(model, args);
            },
            request => Ok(Success(request, request.Message, ResponseMessages.Successful)));
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] ConfirmOtpRequest model)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(
                () => _onBoardingManager.ConfirmOtp(model),
                request => Ok(Success(request, ResponseMessages.Successful)));
        }

        [HttpPost("resend-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                await _onBoardingManager.ResendOnboardingOtp(model, args);
                return "OTP sent";
            },
            request => Ok(Success(request, ResponseMessages.Successful)));
        }

        [HttpPost("create-profile")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateProfile([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(async () =>
            {
                args.OnboardedByUserId = CurrentUserId;
           

                if (string.IsNullOrWhiteSpace(args.UserId))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "UserId must be provided to create a profile for the farmer.");

                if (CurrentUserId != null)
                    await EnrichArgsByRoleAsync(args);

                return await _onBoardingManager.CreateUserProfile(model, args);
            },
            result => Ok(Success(result, ResponseMessages.Successful)));
        }


        [HttpPost("create-organization-profile")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrganization([FromBody] OrganizationOnboardingModel model, [FromQuery] OrganizationOnboardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.onboardedByUserId = CurrentUserId;
                return await _onBoardingManager.CreateOrganization(model, args);
            },
            result => Ok(Success(result, ResponseMessages.Successful)));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model, [FromQuery] string businessCode)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(
                () => _onBoardingManager.Login(model, businessCode),
                createUser =>
                {
                    var response = new Response<TokenModel>
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = true,
                        Message = createUser.Message,
                        Data = createUser.Response
                    };

                    return createUser.Status ? Ok(response) : BadRequest(response);
                });
        }


        [HttpPost("assign-agents-to-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignFarmerToAgents([FromQuery] AssignmentArgs args)
        {
            return await ExecuteAsync(() =>
            {
                args.UserId = CurrentUserId;
                return _onBoardingManager.AssignFarmerToAgents(args);
            },
            request => Ok(Success(request, ResponseMessages.Successful)));
        }

        [HttpPost("assign-cooperatives-to-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignFarmerToCooperatives([FromQuery] AssignmentArgs args)
        {
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(() =>
            {
                args.UserId ??= CurrentUserId;
                args.OnboardedByUserId = CurrentUserId;
               
                return _onBoardingManager.AssignFarmerToCooperatives(args);
            },
            request => Ok(Success(request, ResponseMessages.Successful)));
        }

        private async Task EnrichArgsByRoleAsync(OnBoardingArgs args)
        {
            var roleHandlers = new Dictionary<string, Func<OnBoardingArgs, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Agent"] = EnrichAgentAsync,
                ["MemberShip"] = EnrichCooperativeAsync,              
                // ["Organization"] = EnrichOrganizationAsync,
            };

            foreach (var role in RolePriority)
            {
                if (CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase)
                    && roleHandlers.TryGetValue(role, out var handler))
                {
                    await handler(args);
                    return; // highest-priority match wins
                }
            }
        }

        private async Task EnrichCooperativeAsync(OnBoardingArgs args)
        {
            if (args.Id != 0) return;

            var coop = await _businessManager.GetCooperativeDetailByUserId(CurrentUserId);

            if (coop == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Cooperative not found for the specified user.");

            args.Id = coop.Id;

            args.CooperativeIds ??= new List<long?>();

            if (!args.CooperativeIds.Contains(coop.Id))
                args.CooperativeIds.Add(coop.Id);
        }
        private async Task EnrichAgentAsync(OnBoardingArgs args)
        {
            if (args.Id != 0) return;
            var agent = await _businessManager.GetAgentDetailByUserId(CurrentUserId);

            if (agent == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Agent not found for the specified user.");

            args.Id = agent.Id;

            args.AgentIds ??= new List<long?>();

            if (!args.AgentIds.Contains(agent.Id))
                args.AgentIds.Add(agent.Id);
        }

   
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
                var request = await _onBoardingManager.ForgotPassword(model.Username);

            return Ok(ApiResponse<bool>.Success(
                true,
               ResponseMessages.Successful,
                 ResponseCodes.Success
            ));
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(PasswordResetModel model)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
        var resetPasswordRequest = await _onBoardingManager.ResetPassword(model);

            return Ok(ApiResponse<bool>.Success(
                true,
               ResponseMessages.Successful,
                 ResponseCodes.Success
            ));
        }
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChnagePasswordModel model)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

                model.Email = CurrentUserEmail;
                var resetPasswordRequest = await _onBoardingManager.ChangePassword(model);

            return Ok(ApiResponse<bool>.Success(
                true,
               ResponseMessages.Successful,
                 ResponseCodes.Success
            ));
        }
   
        [HttpPost("forgot-pin")]
        public async Task<IActionResult> ForgotPin([FromBody] ForgotPasswordModel model, [FromQuery] string businessCode, [FromQuery] string Env)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
                var request = await _onBoardingManager.ForgotPin(model.PhoneNumber, businessCode, Env);

            return Ok(ApiResponse<string>.Success(
                request.Message,
               ResponseMessages.Successful,
                 ResponseCodes.Success
            ));
        }
        [HttpPost("reset-pin")]
        public async Task<IActionResult> ResetPin(ConfirmOtpRequest model)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {

                var request = await _onBoardingManager.ResetPin(model);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = request
                });


            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }

        }
        [HttpPost("change-pin")]
        public async Task<IActionResult> ChangePin([FromBody] ChangePinModel model, [FromQuery]OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

                model.PhoneNumber = CurrentPhoneNumber;
                var resetPasswordRequest = await _onBoardingManager.ChangePin(model,args);

            return Ok(ApiResponse<bool>.Success(
                true,
               ResponseMessages.Successful,
                 ResponseCodes.Success
            ));
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var result = await _onBoardingManager.Logout(User);

            if (!result.Status)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
    }
}
