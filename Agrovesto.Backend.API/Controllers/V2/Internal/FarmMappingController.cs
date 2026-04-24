using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
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
                    Message = result != null ? "Successful" : "No Record Found",
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
                    Message = result != null ? "Successful" : "No Record Found",
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
                    Message = result != null ? "Successful" : "No Record Found",
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
                    Message = result != null ? "Successful" : "Farm Details not found",
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
        [Authorize]
        public async Task<IActionResult> CreateFarm(
      [FromForm] SubmitFarmRequest model,
      [FromQuery] FarmMappingArgs args)
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
                    Message = request != null ? "Successful" : "Failed",
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
        }  //private async Task EnrichArgsByRoleAsync(FarmMappingArgs args)
           //{
           //    var currentRole = CurrentUserRole;

        //    switch (currentRole)
        //    {
        //        case "MemberShip":
        //            await EnrichCooperativeAsync(args);
        //            break;

        //        case "Agent":
        //            await EnrichAgentAsync(args);
        //            break;
        //        case "Farmer":
        //            await EnrichFarmerAsync(args);
        //            break;
        //    }
        //}
        //private async Task EnrichFarmerAsync(FarmMappingArgs args)
        //{
        //    //  if (CurrentUserRole != args.userType) return;
        //    if (args.farmerId != 0) return;

        //    var coop = await _businessManager.GetCooperativeDetailByUserId(CurrentUserId);

        //    if (coop == null)
        //        throw new BaseException(
        //            ExceptionType.NOTFOUND,
        //            "Cooperative not found for the specified user.");

        //    args.Id = coop.Id;

        //    args.CooperativeIds ??= new List<long?>();

        //    if (!args.CooperativeIds.Contains(coop.Id))
        //        args.CooperativeIds.Add(coop.Id);
        //}
        //private async Task EnrichCooperativeAsync(FarmMappingArgs args)
        //{

        //    var coop = await _businessManager.GetCooperativeDetailByUserId(CurrentUserId);

        //    if (coop == null)
        //        throw new BaseException(
        //            ExceptionType.NOTFOUND,
        //            "Cooperative not found for the specified user.");

        //    args.Id = coop.Id;

        //    args.CooperativeIds ??= new List<long?>();

        //    if (!args.CooperativeIds.Contains(coop.Id))
        //        args.CooperativeIds.Add(coop.Id);
        //}
        //private async Task EnrichAgentAsync(FarmMappingArgs args)
        //{
        //    //  if(CurrentUserRole != args.userType) return;
        //    if (args.Id != 0) return;

        //    var agent = await _businessManager.GetAgentDetailByUserId(CurrentUserId);

        //    if (agent == null)
        //        throw new BaseException(
        //            ExceptionType.NOTFOUND,
        //            "Agent not found for the specified user.");

        //    args.Id = agent.Id;

        //    args.AgentIds ??= new List<long?>();

        //    if (!args.AgentIds.Contains(agent.Id))
        //        args.AgentIds.Add(agent.Id);
        //}

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
            //  if(CurrentUserRole != args.userType) return;
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
