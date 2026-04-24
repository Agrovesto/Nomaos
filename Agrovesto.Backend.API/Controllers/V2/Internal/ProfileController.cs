using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/profile")]
    [ApiController]
    public class ProfileController : BaseController
    {
        private readonly IOnBoardingManager _onBoardingManager;

        public ProfileController(IHttpContextAccessor httpContextAccessor, 
            IOnBoardingManager onBoardingManager
            ) : base(httpContextAccessor)
        {
            _onBoardingManager = onBoardingManager;
           
        }

        [HttpPost("face-upload")]
        [AllowAnonymous]
        public async Task<IActionResult> SaveFarmerImage(
     [FromForm] ProfileImageModel model,
     [FromQuery] OnBoardingArgs args)
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

            try
            {
                var isCurrentUserFarmer = CurrentUserRoles
                    .Contains("Farmer", StringComparer.OrdinalIgnoreCase);

                if (isCurrentUserFarmer)
                {
                    args.UserId = CurrentUserId;
                }
                else
                {
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
                }

                if (!string.IsNullOrWhiteSpace(OrganizationCode) &&
                    string.IsNullOrWhiteSpace(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }

                var saveFarmerFace = await _onBoardingManager
                    .SaveFarmerImage(model, args);

                return Ok(new Response<object>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
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

            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }

                //if user to be created is a farmer, args.userId has to be cooperative user Id
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

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
                    Message = "Successful",
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

            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }

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
                        Message = "The supplied user is not a Farmer.",
                        Succeeded = false
                    });
                }
                var updateDetail = await _onBoardingManager.RemoveFarmerFromCooperative(args.UserId, args.Id);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
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

            try
            {
                // determine target user id
              
                var targetUserId = !string.IsNullOrEmpty(userId) ? userId : CurrentUserId;

                //if (string.IsNullOrEmpty(targetUserId))
                //{
                //    return BadRequest(new Response<string>
                //    {
                //        Message = "User Id is required.",
                //        Succeeded = false
                //    });
                //}
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
                    Message = "Successful",
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
        public async Task<IActionResult> GetUserProfile([FromQuery] string? userId)
        {
            try
            {
                var targetUserId = !string.IsNullOrEmpty(userId) ? userId : CurrentUserId;
            

                if (string.IsNullOrEmpty(targetUserId))
                {
                    return BadRequest(new Response<string>
                    {
                        Message = "User Id is required.",
                        Succeeded = false
                    });
                }

                var profile = await _onBoardingManager.GetUserProfileAsync(targetUserId);

                return Ok(new Response<UsersProfileResponse>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
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

            try
            {

                //if user to be created is a farmer, args.userId has to be cooperative user Id
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

              

                var updateDetail = await _onBoardingManager.VerifyBvn(args, model, args.UserId);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
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

            try
            {

                //if user to be created is a farmer, args.userId has to be cooperative user Id
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

                var updateDetail = await _onBoardingManager.VerifyNIN(args, model, args.UserId);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
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
            try
            {
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

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
            try
            {

                var request = await _onBoardingManager.ConfirmOtp(model);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
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
            try
            {

                await _onBoardingManager.ResendOnboardingOtp(model, args);


                return Ok(new Response<string>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
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


    }
}
