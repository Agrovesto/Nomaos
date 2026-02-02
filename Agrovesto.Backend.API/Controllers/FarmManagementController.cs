using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
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
        private readonly IBusinessManager _businessManager;
        private readonly IFarmManager _farmManager;
        public FarmManagementController(IHttpContextAccessor httpContextAccessor,
            IFarmPlotDetailsManager farmPlotDetailsManager, IBusinessManager businessManager, IFarmManager farmManager) : base(httpContextAccessor)
        {
            _businessManager = businessManager;
            _farmPlotDetailsManager = farmPlotDetailsManager;
            _farmManager = farmManager;
        }
        [HttpPost("create-farm-map")]
        public async Task<IActionResult> CreateFarmMap([FromBody] CreateFarmDto dto, [FromQuery]BusinessRegistrationArgs args)
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

                var updateDetail = await _farmManager.CreateFarmAsync(dto, args);


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

        [HttpGet("cooperative-farm-map")]
        public async Task<IActionResult> GetCooperativeFarmLand([FromQuery] BusinessRegistrationArgs query)
        {

            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                if (query.CooperativeId == 0 && currentRole == "MemberShip")
                {
                    var coop = await _businessManager.GetCooperativeDetailByUserId(query.UserId);
                    query.CooperativeId = coop.Id;
                }


                var audits = await _farmManager.GetFarmByCooperativeIdAsync(query.CooperativeId);


                return Ok(new Response<List<Farm>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = audits
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


        [HttpGet("cooperatives-farm-map")]
        public async Task<IActionResult> GetCooperativesFarmLand([FromQuery] BusinessRegistrationArgs query)
        {

            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
               
                

                var audits = await _farmManager.GetAllWithCoordinatesAsync(query);


                return Ok(new Response<List<Farm>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = audits
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

                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
               // args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;


                //if user to be created is a farmer, args.userId has to be cooperative user Id
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;
               
                //var updateDetail = await _farmPlotDetailsManager.UpdateFarmPlotDetails(model, args);
                var updateDetail = await _farmPlotDetailsManager.ValidateAndSaveFarm(model, args);


                return Ok(new Response<(bool isValid, string message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = updateDetail.isValid,
                    Message = updateDetail.message,
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


        [HttpPut("unflag-farm/{farmId}")]
        public async Task<IActionResult> UnflagFarm([FromQuery] ListQueryParams args, long farmId)
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
                var currentRole = CurrentUserRole;
                // args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;


                //if user to be created is a farmer, args.userId has to be cooperative user Id
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

                //var updateDetail = await _farmPlotDetailsManager.UpdateFarmPlotDetails(model, args);
                var updateDetail = await _farmPlotDetailsManager.UnflagFarmAsync(args, farmId);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = updateDetail,
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

        [HttpGet("cooperative-plot-details")]
        public async Task<IActionResult> GetPlotDetails([FromQuery] ListQueryParams query)
        {

            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;


                if (!query.CooperativeId.HasValue)
                {
                    var coop = await _businessManager.GetCooperativeDetailByUserId(query.UserId);
                    query.CooperativeId = coop.Id;
                }


                var plots = await _farmPlotDetailsManager.GetFarmDetailsByCooperative(query);


                return Ok(new Response<List<FarmPlotDetailsModel>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = plots
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
        [HttpGet("business-plot-details")]
        public async Task<IActionResult> GetBusinessPlotDetails([FromQuery] ListQueryParams query)
        {

            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;


                if (!query.CooperativeId.HasValue && currentRole == "MemberShip")
                {
                    var coop = await _businessManager.GetCooperativeDetailByUserId(query.UserId);
                    query.CooperativeId = coop.Id;
                }


                var plots = await _farmPlotDetailsManager.GetFarmDetailsByBusiness(query);


                return Ok(new Response<List<FarmPlotDetailsModel>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = plots
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

        [HttpGet("plot-audit/{plotId}")]
        public async Task<IActionResult> GetAuditTrail([FromQuery] BusinessRegistrationArgs query, long plotId)
        {

            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;


                var audits = await _farmPlotDetailsManager.GetAuditTrail(query, plotId);


                return Ok(new Response<List<FarmFlagAuditTrailModel>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = audits
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

                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
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
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
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

                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
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
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
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
        public async Task<IActionResult> GetVisitationLogsByOfficerInCharge([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
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
