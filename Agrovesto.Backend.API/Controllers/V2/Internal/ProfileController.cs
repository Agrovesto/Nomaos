using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/profile")]
    [ApiController]
    [Authorize]

    [AllowPendingCredentialChange]
    public class ProfileController : BaseController
    {
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly IProfileAccessPolicy _accessPolicy;
        public ProfileController(IHttpContextAccessor httpContextAccessor,
            IProfileAccessPolicy accessPolicy,
            IOnBoardingManager onBoardingManager
            ) : base(httpContextAccessor)
        {
            _onBoardingManager = onBoardingManager;
            _accessPolicy = accessPolicy;
        }

        [HttpPost("face-upload")]
        [AllowAnonymous]
        public async Task<IActionResult> SaveFarmerImage(
        [FromForm] ProfileImageModel model,
        [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();


            try
            {
                var isCurrentUserFarmer = CurrentUserRoles
                    .Contains("Farmer", StringComparer.OrdinalIgnoreCase);

                    args.UserId = args.UserId ?? CurrentUserId;


              
                    if (string.IsNullOrWhiteSpace(args.UserId))
                    {
                        return BadRequest(new Response<string>()
                        {
                            Message = "Farmer User Id is required.",
                            Succeeded = false
                        });
                    }

                    if (!await _onBoardingManager.IsUserInRoleAsync(args.UserId, "Farmer"))
                    {
                        return StatusCode(400, new Response<string>
                        {
                            Message = "The supplied user is not a Farmer.",
                            Succeeded = false
                        });
                    }
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;

                var saveFarmerFace = await _onBoardingManager
                    .SaveFarmerImage(model, args);

                return Ok(new Response<object>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = saveFarmerFace
                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false
                });
            }
        }
        
        [HttpPost("map-farm")]
        public async Task<IActionResult> CreateFarmMap([FromBody] CreateFarmDto dto, [FromQuery] BusinessRegistrationArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();


            try
            {
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;

                //if user to be created is a farmer, args.userId has to be cooperative user Id
                args.UserId = args.UserId ?? CurrentUserId;
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed, User Id missing.",
                        Succeeded = false,
                        Data = ModelState
                      .Where(ms => ms.Value.Errors.Any())
                      .ToDictionary(
                          ms => ms.Key,
                          ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                      )
                    });
                }
               
                var updateDetail = await _onBoardingManager.UpdateFarmDetails(dto, args);


                return Ok(new Response<CreateFarmDto>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = updateDetail
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

        //remove farmer
        [HttpPost("remove-farmer-from-cooperative")]
        public async Task<IActionResult> RemoveFarmerFromCooperative([FromQuery] Params args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();


            try
            {
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;

                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
             
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed, User Id missing.",
                        Succeeded = false,
                        Data = ModelState
                      .Where(ms => ms.Value.Errors.Any())
                      .ToDictionary(
                          ms => ms.Key,
                          ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                      )
                    });
                }
                if (!await _onBoardingManager.IsUserInRoleAsync(args.UserId, "Farmer"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The user is not a Farmer.",
                        Succeeded = false
                    });
                }
                var updateDetail = await _onBoardingManager.RemoveFarmerFromCooperative(args.UserId, args.Id, args.organizationCode);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = updateDetail
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

        //edit farmer profile

        [HttpPut("update-farmer-profile")]
        public async Task<IActionResult> UpdateFarmerProfile([FromBody] FarmerUpdateModel model, [FromQuery] string? userId)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();


            try
            {
              
                var targetUserId = !string.IsNullOrEmpty(userId) ? userId : CurrentUserId;

              
                if (!await _onBoardingManager.IsUserInRoleAsync(targetUserId, "Farmer"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a Farmer.",
                        Succeeded = false
                    });
                }
                var result = await _onBoardingManager.UpdateFarmerProfile(targetUserId, model);

                return Ok(new Response<Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateResultDto>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = result.Success,
                    Message = result.Message,
                    Data = result
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }

        //fetch farmer detail

        [HttpGet("farmer")]
        public async Task<IActionResult> GetFarmerProfile([FromQuery] string? userId)
        {
            try
            {
                userId = !string.IsNullOrEmpty(userId) ? userId : CurrentUserId;
                if (!await _onBoardingManager.IsUserInRoleAsync(userId, "Farmer"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a Farmer.",
                        Succeeded = false
                    });
                }
                var profile = await _onBoardingManager.GetFarmerProfile(userId);

                return Ok(new Response<FarmerProfileDto>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = profile
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }



        ///
        /// 
        /// ///
        ////get current logged in user profile to include name, phone number, email, picture, id, level/tier
         [HttpGet("user-profile")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserProfile([FromQuery] string? userId)
        {
            try
            {
                var targetUserId = !string.IsNullOrEmpty(userId) ? userId : CurrentUserId;

                if (string.IsNullOrEmpty(targetUserId))
                {
                    return Unauthorized(new Response<string>
                    {
                        Message = "User could not be resolved from the supplied token.",
                        Succeeded = false
                    });
                }

                var profile = await _onBoardingManager.GetUserProfileAsync(targetUserId);

                return Ok(new Response<UsersProfileResponse>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = profile
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }


        [HttpPost("verify-bvn")]
        public async Task<IActionResult> VerifyBVN([FromBody] KYCVerificationDto model, [FromQuery] VerificationArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {

                //if user to be created is a farmer, args.userId has to be cooperative user Id
                args.UserId = args.UserId ?? CurrentUserId;


                var updateDetail = await _onBoardingManager.VerifyBvn(args, model, args.UserId);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = updateDetail
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

        [HttpPost("verify-nin")]
        public async Task<IActionResult> VerifyNIN([FromBody] KYCVerificationDto model, [FromQuery] VerificationArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {

                //if user to be created is a farmer, args.userId has to be cooperative user Id
                args.UserId = args.UserId ?? CurrentUserId;
                var updateDetail = await _onBoardingManager.VerifyNIN(args, model, args.UserId);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = updateDetail
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
        [HttpPost("request-otp")]
        [Authorize]
        public async Task<IActionResult> RequestEmailOrPhoneVerification([FromQuery] VerificationArgs args, [FromBody]KYCVerificationDto model)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
                args.UserId = args.UserId ?? CurrentUserId;
                var request = await _onBoardingManager.RequestEmailOrPhoneVerification(args, model, args.UserId);


                return Ok(new Response<string>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = $"OTP has been sent to your provided details {request}",
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
        [HttpPost("verify-otp")]
        [Authorize]
        public async Task<IActionResult> VerifyOtp([FromBody] ConfirmOtpRequest model)
        {

            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {

                var request = await _onBoardingManager.ConfirmOtp(model);


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
        [HttpPost("resend-otp")]
        [Authorize]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {

                await _onBoardingManager.ResendOnboardingOtp(model, args);


                return Ok(new Response<string>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = "OTP sent"
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

        [HttpPut("update-agent-profile")]
        public async Task<IActionResult> UpdateAgentProfile([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();


            try
            {

                var targetUserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

                 if (!await _onBoardingManager.IsUserInRoleAsync(targetUserId, "Farmer"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a Farmer.",
                        Succeeded = false
                    });
                }
                var result = await _onBoardingManager.EditAgentUser(model, args);

                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = result,
                    Message = ResponseCodes.Success,
                    Data = result
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }


        [HttpPatch("update-profile")]
        [ProducesResponseType(typeof(ProfileUpdateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, [FromQuery] ProfileUpdateArgs query)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(callerId)) return Unauthorized();

          
            var targetUserId = string.IsNullOrWhiteSpace(query.UserId) ? callerId : query.UserId.Trim();
            var isSelf = string.Equals(targetUserId, callerId, StringComparison.Ordinal);
           

            if (!TryResolveRole(query.Role, isSelf, out var role, out var roleError))
                return BadRequest(new { message = roleError });

            if (!await _accessPolicy.CanEditAsync(User, targetUserId, role, query.BusinessCode))
                return Forbid();

              var args = new OnBoardingArgs
            {
                UserId = targetUserId,
                businessCode = query.BusinessCode,
                organizationCode = query.OrganizationCode,
                       OnboardedByUserId = !isSelf && User.IsInRole(DefaultProfileAccessPolicy.AgentRole) ? callerId : null
            };
           
            var result = await _onBoardingManager.UpdateProfileAsync(role, args, request.Profile, request.Farmer, request.Organization);

            return Ok(new Response<IReadOnlyList<string>>
            {
                Data=result.UpdatedFields,
                Code=ResponseCodes.Success,
                Succeeded=result.Success,
                Message=result.Message
            });
        }
        private bool TryResolveRole(string? requested, bool isSelf, out ProfileRole role, out string? error)
        {
            role = default;
            error = null;
            requested = requested?.Trim();

            // 1. A role was explicitly requested: validate it.
            if (!string.IsNullOrWhiteSpace(requested))
            {
                if (!Enum.TryParse(requested, ignoreCase: true, out role) || !Enum.IsDefined(role))
                {
                    error = $"Role must be one of: {string.Join(", ", Enum.GetNames<ProfileRole>())}.";
                    return false;
                }

                // For self-edits, the caller must actually hold the role they're asking for.
                if (isSelf && !GetHeldProfileRoles().Contains(role))
                {
                    error = "You do not hold that role.";
                    return false;
                }

                return true;
            }

            // 2. No role requested: only allowed for self-edits.
            if (!isSelf)
            {
                error = "Role is required when updating another user's profile.";
                return false;
            }

            // 3. Self-edit with no role: infer it if the caller holds exactly one profile role.
            var held = GetHeldProfileRoles();

            if (held.Count == 1)
            {
                role = held[0];
                return true;
            }

            error = held.Count == 0
                ? "Your account has no updatable profile role."
                : "You hold more than one role. Specify which profile to update.";
            return false;
        }

        private List<ProfileRole> GetHeldProfileRoles() =>
      User.FindAll("Roles")
          .Select(c => Enum.TryParse<ProfileRole>(c.Value, ignoreCase: true, out var r) && Enum.IsDefined(r)
              ? (ProfileRole?)r
              : null)
          .Where(r => r.HasValue)
          .Select(r => r!.Value)
          .Distinct()
          .ToList();
    }
}
