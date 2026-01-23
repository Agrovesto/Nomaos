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

        public ProfileController(IHttpContextAccessor httpContextAccessor, IOnBoardingManager onBoardingManager
            ) : base(httpContextAccessor)
        {
            _onBoardingManager = onBoardingManager;
           
        }

        [HttpPost("face-upload")]
        [AllowAnonymous]
        public async Task<IActionResult> SaveFarmerImage([FromForm] ProfileImageModel model, [FromQuery] OnBoardingArgs args)
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
                if (CurrentUserRole != "Farmer" && string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed, Id missing.",
                        Succeeded = false,
                        Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                    });
                }
                if (CurrentUserRole == "Farmer")
                {
                    args.UserId = CurrentUserId;
                }

                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                var saveFarmerFace = await _onBoardingManager.SaveFarmerImage(model, args);


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


    }
}
