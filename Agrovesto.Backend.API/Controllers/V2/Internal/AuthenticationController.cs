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

        [Authorize]
        [HttpGet("notification-preference")]
        public async Task<IActionResult> GetNotificationPreference([FromQuery] string? role = null)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Unauthorized(new Response<string>
                {
                    Message = "User could not be resolved from the supplied token.",
                    Succeeded = false
                });
            }

            var resolutionError = TryResolveNotificationRole(role, out var resolvedRole);
            if (resolutionError != null) return resolutionError;

            return await ExecuteAsync(
                () => _onBoardingManager.GetNotificationPreferenceAsync(CurrentUserId, resolvedRole),
                result => Ok(new Response<NotificationPreferenceResponse>
                {
                    Data = result,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Code = ResponseCodes.Success
                }));
        }

        [Authorize]
        [HttpPost("notification-preference")]
        public async Task<IActionResult> SaveNotificationPreference(
            [FromBody] NotificationPreferenceRequest model,
            [FromQuery] string? role = null)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Unauthorized(new Response<string>
                {
                    Message = "User could not be resolved from the supplied token.",
                    Succeeded = false
                });
            }

            var resolutionError = TryResolveNotificationRole(role, out var resolvedRole);
            if (resolutionError != null) return resolutionError;

            return await ExecuteAsync(
                () => _onBoardingManager.SaveNotificationPreferenceAsync(CurrentUserId, resolvedRole, model),
                result => Ok(new Response<NotificationPreferenceResponse>
                {
                    Data = result,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Code = ResponseCodes.Success
                }));
        }

        private IActionResult? TryResolveNotificationRole(string? suppliedRole, out NotificationUserRole resolvedRole)
        {
            resolvedRole = default;

            var eligibleRoles = Enum.GetValues<NotificationUserRole>()
                .Where(r => CurrentUserRoles.Contains(r.ToString(), StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (!string.IsNullOrWhiteSpace(suppliedRole))
            {
                if (!Enum.TryParse<NotificationUserRole>(suppliedRole, ignoreCase: true, out var parsedRole))
                {
                    return BadRequest(ApiResponse<List<string>>.Fail(
                        $"'{suppliedRole}' is not a notification role. Use one of: {string.Join(", ", Enum.GetNames<NotificationUserRole>())}.",
                        ResponseCodes.Failed));
                }

                if (!eligibleRoles.Contains(parsedRole))
                {
                    return Unauthorized(ApiResponse<object>.Fail(
                        ResponseMessages.AccessDenied,
                        ResponseCodes.AccessDenied));
                }

                resolvedRole = parsedRole;
                return null;
            }

            if (eligibleRoles.Count == 0)
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied));
            }

            if (eligibleRoles.Count > 1)
            {
                var response = ApiResponse<List<string>>.Fail(
                    "You belong to multiple roles. Please specify a role to continue.",
                    ResponseCodes.RoleSelectionRequired);
                response.Data = eligibleRoles.Select(r => r.ToString()).ToList();
                return Unauthorized(response);
            }

            resolvedRole = eligibleRoles[0];
            return null;
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
        [AllowPendingCredentialChange]
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
        [AllowPendingCredentialChange]
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
        [AllowPendingCredentialChange]
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
        [AllowPendingCredentialChange]
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
        [AllowPendingCredentialChange]
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
        [AllowPendingCredentialChange]
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
                
        [Authorize]
        [HttpPost("logout")]
        [AllowPendingCredentialChange]
        public async Task<IActionResult> Logout()
        {
            var result = await _onBoardingManager.Logout(User);

            if (!result.Status)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
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

        [HttpPost("change-credential")]
        [AllowPendingCredentialChange]
        public async Task<IActionResult> ChangeCredential([FromBody] ChangeCredentialRequest request)
        {
            var model = new ChangeCredentialModel
            {
                Type = request.Type,
                OldValue = request.OldValue,
                NewValue = request.NewValue,
                ConfirmValue = request.ConfirmValue,
                Email = request.Type == CredentialType.Password
                    ? User.FindFirstValue(ClaimTypes.Email)
                    : null,
                PhoneNumber = request.Type == CredentialType.Pin
                    ? User.FindFirstValue(ClaimTypes.MobilePhone)
                    : null
            };
            var (status, message) = await _onBoardingManager.ChangeCredential(model);
            return Ok(new { status, message });
        }

        [HttpPost("forgot-credential")]
        [AllowPendingCredentialChange]
        public async Task<IActionResult> ForgotCredential([FromBody] ForgotCredentialRequest request,
            [FromQuery] string businessCode,
            [FromQuery] string env)
        {
            var model = new ForgotCredentialModel
            {
                Type = request.Type,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                BusinessCode = businessCode,
                Env=env
            };

            var (status, message) = await _onBoardingManager.ForgotCredential(model);
            return Ok(new { status, message });
        }
        [HttpPost("reset-credential")]
        [AllowPendingCredentialChange]
        public async Task<IActionResult> ResetCredential([FromBody] ConfirmCredentialResetRequest request)
        {
            var model = new ConfirmCredentialResetModel
            {
                Type = request.Type,
                PhoneNumber = request.PhoneNumber,
                Code = request.Code,
                UserId = request.UserId,
                Token = request.Token,
                NewValue = request.NewValue,
                ConfirmValue = request.ConfirmValue
            };

            var (status, message) = await _onBoardingManager.ConfirmReset(model);
            return Ok(new { status, message });
        }
    }
}

