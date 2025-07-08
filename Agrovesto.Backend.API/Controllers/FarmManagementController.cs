using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/farm/activities")]
    [ApiController]
    public class FarmManagementController : BaseController
    {
        private readonly IFarmPlotDetailsManager _farmPlotDetailsManager;

        public FarmManagementController(IHttpContextAccessor httpContextAccessor,
            IFarmPlotDetailsManager farmPlotDetailsManager) : base(httpContextAccessor)
        {
            
            _farmPlotDetailsManager = farmPlotDetailsManager;
        }
        [HttpPost("farm-plot-details")]    
        public async Task<IActionResult> UpdateFarmCoordinateDetails([FromBody] FarmPlotDetailsModel model, [FromQuery] BusinessRegistrationArgs args)
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
               
                var updateDetail = await _farmPlotDetailsManager.UpdateFarmCoordinateDetails(model, args);
                

                return Ok(new Response<FarmPlotDetailsModel>()
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
         [HttpPost("update-plot-details")]    
        public async Task<IActionResult> UpdateFarmPlotDetails([FromBody] FarmPlotDetailsModel model, [FromQuery] BusinessRegistrationArgs args)
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
               
                var updateDetail = await _farmPlotDetailsManager.UpdateFarmPlotDetails(model, args);
                

                return Ok(new Response<FarmPlotDetailsModel>()
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


        [HttpGet("plot-details")]
        public async Task<IActionResult> GetPlotDetails([FromQuery] long farmerId)
        {

            try
            {
     
                var updateDetail = await _farmPlotDetailsManager.GetFarmDetailByFarmer(farmerId);


                return Ok(new Response<List<FarmPlotDetailsModel>>()
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

        [HttpPost("create-activity-type")]
        public async Task<IActionResult> CreateActivityType([FromBody] FarmActivityTypeModel model, [FromQuery] BusinessRegistrationArgs args)
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
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    args.organizationCode = OrganizationCode;
                }

                var updateDetail = await _farmPlotDetailsManager.CreateActivityType(model, args);


                return Ok(new Response<FarmActivityTypeModel>()
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


        [HttpGet("activity-types")]
        public async Task<IActionResult> GetActivityTypes([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    query.organizationCode = OrganizationCode;
                }

                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmPlotDetailsManager.GetActivityTypes(query);
                return Ok(new PagedListResponse<List<FarmActivityTypeModel>>()
                {
                    Data = result,
                    Message = "Successful",
                    Succeeded = true,
                    Count = result.Count,
                    //PageNumber = query.PageNumber,
                    //PageSize = 10,
                    Code =  Core.CoreUtils.Constants.ResponseCodes.Success 
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
        
        [HttpGet("activity-type/{activityId}")]
        public async Task<IActionResult> GetPlotDetails([FromQuery] BusinessRegistrationArgs query, long activityId)
        {

            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var updateDetail = await _farmPlotDetailsManager.GetActivityTypeById(query, activityId);


                return Ok(new Response<FarmActivityTypeModel>()
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


        [HttpPost("log-activity")]
        public async Task<IActionResult> LogFarmVisitation([FromBody] FarmVisitationActivityModel model, [FromQuery] BusinessRegistrationArgs args)
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
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    args.organizationCode = OrganizationCode;
                }

                //if user to be created is a farmer, args.userId has to be cooperative user Id
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

                var updateDetail = await _farmPlotDetailsManager.LogFarmVisit(model, args);


                return Ok(new Response<FarmVisitationActivityModel>()
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

        [HttpGet("business-visitation-logs")]
        public async Task<IActionResult> GetVisitationLogsByBusiness([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    query.organizationCode = OrganizationCode;
                }

                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmPlotDetailsManager.GetVisitationLogsByBusiness(query);
                return Ok(new PagedListResponse<List<FarmVisitationActivityModel>>()
                {
                    Data = result,
                    Message = "Successful",
                    Succeeded = true,
                    Count = result.Count,
                    //PageNumber = query.PageNumber,
                    //PageSize = 10,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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

        [HttpGet("visitation-logs-by-farmer")]
        public async Task<IActionResult> GetVisitationLogByFarmer([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                if(CurrentUserRole == "Fsrmer")
                {
                    query.UserId = CurrentUserId;
                }
                var result = await _farmPlotDetailsManager.GetVisitationLogByFarmer(query, query.UserId);
                return Ok(new PagedListResponse<List<FarmVisitationActivityModel>>()
                {
                    Data = result,
                    Message = "Successful",
                    Succeeded = true,
                    Count = result.Count,
                    //PageNumber = query.PageNumber,
                    //PageSize = 10,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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

        [HttpGet("business-visitation-logs-by-agent")]
        public async Task<IActionResult> GetVisitationLogsByOfficerInCharge([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmPlotDetailsManager.GetVisitationLogsByOfficerInCharge(query);
                return Ok(new PagedListResponse<List<FarmVisitationActivityModel>>()
                {
                    Data = result,
                    Message = "Successful",
                    Succeeded = true,
                    Count = result.Count,
                    //PageNumber = query.PageNumber,
                    //PageSize = 10,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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

        [HttpGet("business-visitation-logs-by-cooperative")]
        public async Task<IActionResult> GetVisitationLogsByCooperative([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmPlotDetailsManager.GetVisitationLogsByCooperative(query);
                return Ok(new PagedListResponse<List<FarmVisitationActivityModel>>()
                {
                    Data = result,
                    Message = "Successful",
                    Succeeded = true,
                    Count = result.Count,
                    //PageNumber = query.PageNumber,
                    //PageSize = 10,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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

        [HttpGet("business-visitation-logs-by-type/{activityTypeId}")]
        public async Task<IActionResult> GetVisitationLogsByActivityType([FromQuery] BusinessRegistrationArgs query, long activityTypeId)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmPlotDetailsManager.GetVisitationLogsByActivityType(query, activityTypeId);
                return Ok(new PagedListResponse<List<FarmVisitationActivityModel>>()
                {
                    Data = result,
                    Message = "Successful",
                    Succeeded = true,
                    Count = result.Count,
                    //PageNumber = query.PageNumber,
                    //PageSize = 10,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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


        [HttpGet("visitation/{activityId}")]
        public async Task<IActionResult> GetVisitationLogById([FromQuery] BusinessRegistrationArgs query, long activityId)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var updateDetail = await _farmPlotDetailsManager.GetVisitationLogById(query, activityId);

                return Ok(new Response<FarmVisitationActivityModel>()
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
