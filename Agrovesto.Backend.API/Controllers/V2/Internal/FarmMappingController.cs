using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/farms")]
    [ApiController]
    public class FarmMappingController : BaseController
    {
        private readonly IFarmMappingManager _farmMappingManager;
        private readonly IBusinessManager _businessManager;
        private readonly IOnBoardingManager _onBoardingManager;
        //POST /api/farms
        //POST /api/farms/{farmId}/map
        //POST /api/farms/{farmId}/documents

        //verify mapping
        //verify document
        //verify land size
        //unflag farm
        public FarmMappingController(IHttpContextAccessor httpContextAccessor, 
            IFarmMappingManager farmMappingManager,
            IBusinessManager businessManager, IOnBoardingManager onBoardingManager) : base(httpContextAccessor)
        {
            _farmMappingManager = farmMappingManager;
            _businessManager = businessManager;
            _onBoardingManager = onBoardingManager;
        }
        [HttpGet("by-membership")]
        public async Task<IActionResult> FetchAllFarmsByFarmersBelongingToAMembership([FromQuery] FarmMappingArgs query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.userId = !string.IsNullOrEmpty(query.userId) ? query.userId : CurrentUserId;
                if(string.IsNullOrEmpty(query.userId))
                {
                    throw new BaseException(ExceptionType.ACCESS_DENIED, "Access Denied");
                }
                if (!await _onBoardingManager.IsUserInRoleAsync(query.userId, "MemberShip"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a MemberShip.",
                        Succeeded = false
                    });
                }
                var result = await _farmMappingManager.FetchAllFarmsByFarmersBelongingToAMembership(query);
                return Ok(new Response<List<FarmListRespopnseObject>>()
                {
                    Data = result,

                    Message = result != null ? ResponseMessages.Successful : ResponseMessages.NotFound,
                    Succeeded = result != null,
                    Code = (result != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpGet("by-agent")]
        public async Task<IActionResult> FetchAllFarmsByFarmersBelongingToAnAgent([FromQuery] FarmMappingArgs query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
              

                query.userId = !string.IsNullOrEmpty(query.userId) ? query.userId : CurrentUserId;

                if (!await _onBoardingManager.IsUserInRoleAsync(query.userId, "Agent"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not an Agent.",
                        Succeeded = false
                    });
                }
                var result = await _farmMappingManager.FetchAllFarmsByFarmersBelongingToAnAgent(query);
                return Ok(new Response<List<FarmListRespopnseObject>>()
                {
                    Data = result,

                    Message = result != null ? ResponseMessages.Successful : ResponseMessages.NotFound,
                    Succeeded = result != null,
                    Code = (result != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpGet("by-farmer")]
        public async Task<IActionResult> FetchFarmsByFarmer([FromQuery] FarmMappingArgs query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var isFarmer = CurrentUserRoles
             .Contains("Farmer", StringComparer.OrdinalIgnoreCase);

                if (isFarmer)
                {
                    await EnrichFarmerAsync(query);

                    if (query.farmerId <= 0)
                    {
                        return BadRequest(new Response<string>
                        {
                            Message = "Unable to resolve farmer profile for current user.",
                            Succeeded = false
                        });
                    }
                }
                else
                {
                    if (query.farmerId <= 0)
                    {
                        return BadRequest(new Response<string>
                        {
                            Message = "Farmer Id is required.",
                            Succeeded = false
                        });
                    }

                    await _onBoardingManager.EnsureFarmerIdIsValidAsync(query.farmerId);
                }




                var result = await _farmMappingManager.FetchFarmsByFarmer(query);
                return Ok(new Response<List<FarmListRespopnseObject>>()
                {
                    Data = result,

                    Message = result != null ? ResponseMessages.Successful : ResponseMessages.NotFound,
                    Succeeded = result != null,
                    Code = (result != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpGet("details")]
        public async Task<IActionResult> FetchFarmDetails([FromQuery] FarmMappingArgs query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
             if(query.farmId <= 0)
                {

                    return BadRequest(new Response<string>
                    {
                        Message = "Farm Id is required.",
                        Succeeded = false
                    });
                }



                var result = await _farmMappingManager.FetchFarmDetails(query);
                return Ok(new Response<FarmDetailsResponse>()
                {
                    Data = result,
                    Message = result != null ? ResponseMessages.Successful : ResponseMessages.NotFound,
                    Succeeded = result != null,
                    Code = (result != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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


        [HttpPost("submit-farm")]
   
        public async Task<IActionResult> CreateFarm(
      [FromForm] SubmitFarmRequest model,
      [FromQuery] FarmMappingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();


            try
            {
                args.performedByUserId = CurrentUserId;
                NormalizeOrganizationCode(args);

                var isFarmer = CurrentUserRoles
                    .Contains("Farmer", StringComparer.OrdinalIgnoreCase);

                if (isFarmer)
                {
                    await EnrichFarmerAsync(args);

                    if (args.farmerId <= 0)
                    {
                        return BadRequest(new Response<string>
                        {
                            Message = "Unable to resolve farmer profile for current user.",
                            Succeeded = false
                        });
                    }
                }
                else
                {
                    if (args.farmerId <= 0)
                    {
                        return BadRequest(new Response<string>
                        {
                            Message = "Farmer Id is required.",
                            Succeeded = false
                        });
                    }

                    await _onBoardingManager.EnsureFarmerIdIsValidAsync(args.farmerId);
                }

                var request = await _farmMappingManager.SubmitFarmAsync(model, args);

                return Ok(new Response<long>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = request < 1 ? ResponseMessages.Successful : ResponseMessages.Failed,
                    Data = request
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

        private void NormalizeOrganizationCode(FarmMappingArgs args)
        {
            if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
            {
                args.organizationCode = OrganizationCode;
            }
        }

      
         private async Task EnrichFarmerAsync(FarmMappingArgs args)
        {
            if (args.farmerId != 0) return;

            var farmer = await _businessManager.GetFarmerByUserId(CurrentUserId);

            if (farmer == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Farmer not found for the specified user.");

            args.farmerId = farmer.Id;
        }
    }
}
