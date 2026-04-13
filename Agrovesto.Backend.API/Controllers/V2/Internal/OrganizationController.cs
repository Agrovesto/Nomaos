using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/organization")]
    [ApiController]
    public class OrganizationController : BaseController
    {

        private readonly IBusinessManager _businessManager;
        private readonly IOrganizationManager _organizationManager;
        public OrganizationController(IHttpContextAccessor httpContextAccessor,
            IBusinessManager businessManager,
             IOrganizationManager organizationManager) : base(httpContextAccessor)
        {
            _organizationManager = organizationManager;
            _businessManager = businessManager;
        }

        #region AgentMgt

         [HttpGet("agent-mgt-overview")]

        public async Task<IActionResult> GetAgentManagementOverview([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = !string.IsNullOrEmpty(query.organizationCode) ? query.organizationCode : OrganizationCode;

                if (string.IsNullOrEmpty(query.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var overview = await _organizationManager.GetOrganizationDashboardAsync(query);
                return Ok(new Response<OrganizationDashboardResponse>()
                {
                    Code = (overview != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = "Successful",
                    Data = overview
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

        [HttpPatch("edit-location")]
        public async Task<IActionResult> UpdateLocationAsync([FromBody] OrganizationLocationModel model, [FromQuery] ListQueryParams args)
        {
            var isAdmin = CurrentUserRole;
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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

                args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.UpdateLocationAsync(model, args);

                return Ok(new Response<OrganizationLocationModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [HttpPost("create-location")]
        public async Task<IActionResult> Location([FromBody] OrganizationLocationModel model, [FromQuery] ListQueryParams args)
        {
            var isAdmin = CurrentUserRole;
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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

                args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.CreateLocation(model, args);

                return Ok(new Response<OrganizationLocationModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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
        [HttpGet("locations")]

        public async Task<IActionResult> GetLocations([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.GetLocationAsync(args);
                return Ok(new Response<List<OrganizationLocationResponseModel>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [HttpGet("location")]

        public async Task<IActionResult> GetLocation([FromQuery] ListQueryParams query)
        {
            try
            {

                var type = await _organizationManager.GetLocationById(query);
                return Ok(new Response<OrganizationLocationModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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


        [HttpGet("location-inventory")]
        public async Task<IActionResult> GetInventoryByLocationAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
              
                var response = await _organizationManager.GetLocationInventoryBreakdownAsync(args);
                return Ok(new PagedListResponse<List<LocationInventoryBreakdownResponse>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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

        [HttpGet("location-activity")]
        public async Task<IActionResult> GetLocationActivityLogsAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var response = await _organizationManager.GetLocationActivityLogsAsync(args);
                return Ok(new PagedListResponse<List<LocationActivityLogResponse>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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

        [HttpGet("location-agents")]
        public async Task<IActionResult> GetAgentsByLocationAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
              
                var response = await _organizationManager.GetAgentsByLocationAsync(args);
                return Ok(new PagedListResponse<List<LocationAgentDetailResponse>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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

        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole([FromBody] OrganizationRoleModel model, [FromQuery] ListQueryParams args)
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

                args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.CreateRole(model, args);

                return Ok(new Response<OrganizationRoleModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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
        [HttpGet("roles")]

        public async Task<IActionResult> GetRolesAsync([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.GetRolesAsync(args);
                return Ok(new Response<List<OrganizationRoleModel>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [HttpGet("role")]

        public async Task<IActionResult> GetRoleById([FromQuery] ListQueryParams query)
        {
            try
            {

                var type = await _organizationManager.GetRoleById(query);
                return Ok(new Response<OrganizationRoleModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

      
        //create-agent
        [HttpPost("create-agent")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateAgentUser([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
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
                args.OnboardedByUserId = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                NormalizeOrganizationCode(args);


                var request = await _organizationManager.CreateAgentUser(model, args);


                return Ok(new Response<CreateAgentResponseModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = request.Message,
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
        [HttpGet("agents")]
        public async Task<IActionResult> GetOrganizationAgents([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;



                var result = await _organizationManager.GetOrganizationAgents(query);
                return Ok(new PagedListResponse<List<OrganizationAgentResponse>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = (result.Succeeded && result.Data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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
        [HttpGet("agent-detail")]
        public async Task<IActionResult> GetAgentDetail([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentDetail(args);
            return Ok(response);
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
        [HttpGet("agent-locations")]
        public async Task<IActionResult> GetAgentLocationAssignment([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentAssignedLocations(args);
                return Ok(new PagedListResponse<List<OrganizationAgentLocationResponseModel>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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


        [HttpPost("change-agent-location")]
        [AllowAnonymous]
        public async Task<IActionResult> ChangeAgentLocation([FromBody] ProfileRequestModel model,
            [FromQuery] OrganizationAgentArgs args)
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
                args.OnboardedById = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                if (!string.IsNullOrEmpty(OrganizationCode) &&
              string.IsNullOrEmpty(args.OrganizationCode))
                {
                    args.OrganizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.OrganizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }


                var request = await _organizationManager.ReassignAgentToLocation(model, args);


                return Ok(new Response<bool>()
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


        [HttpPost("assign-agent-location")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignAgentLocation([FromBody] ProfileRequestModel model,
           [FromQuery] OrganizationAgentArgs args)
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
                args.OnboardedById = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                if (!string.IsNullOrEmpty(OrganizationCode) &&
              string.IsNullOrEmpty(args.OrganizationCode))
                {
                    args.OrganizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.OrganizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
        await _organizationManager.AssignAgentLocation(model, args);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful"
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


        [HttpPost("assign-agent-role")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignAgentRole([FromBody] ProfileRequestModel model,
           [FromQuery] OrganizationAgentArgs args)
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
                args.OnboardedById = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                if (!string.IsNullOrEmpty(OrganizationCode) &&
              string.IsNullOrEmpty(args.OrganizationCode))
                {
                    args.OrganizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.OrganizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                await _organizationManager.AssignAgentToTole(model, args);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful"
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


        [HttpGet("agent-activity-log")]
        public async Task<IActionResult> GetAgentActivityLogs([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentActivityLogs(args);
                return Ok(new PagedListResponse<List<OrganizationAgentActivityLog>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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


        [HttpGet("agent-transaction-history")]
        public async Task<IActionResult> GetAgentTransactionHistory([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentTransactionHistory(args);
                return Ok(new PagedListResponse<List<OrganizationAgentTransactionHistory>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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

      
        [HttpGet("agent-performance-summary")]
        public async Task<IActionResult> GetOrganizationAgentPerformanceSummary([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetOrganizationAgentPerformanceSummary(args);
                return Ok(new Response<OrganizationAgentPerformanceSummary>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response
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

        [HttpGet("agent-subscribers")]
        public async Task<IActionResult> GetAgentSubscribers([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentSubscribers(args);
                return Ok(new PagedListResponse<List<OrganizationAgentSubscriberModel>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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


        [HttpGet("agent-input-allocation")]
        public async Task<IActionResult> GetAgentInputAllocations([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentInputAllocations(args);
             
                return Ok(new PagedListResponse<List<OrganizationAgentInputAllocationModel>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count=response.Count,   
                    PageSize=args.PageSize,
                    PageNumber=args.PageNumber
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

        [HttpPost("assign-agent")]
        public async Task<IActionResult> AssignAgentToOrganization([FromBody] OrganizationAgentAssignmArgs model,
          [FromQuery] OrganizationAgentAssignmArgs args)
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
          
                if (!string.IsNullOrEmpty(OrganizationCode) &&
              string.IsNullOrEmpty(args.OrganizationCode))
                {
                    model.OrganizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(model.OrganizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                model.LocationId = args.Id;
                model.OnboardedById = CurrentUserId;
                model.BusinessCode = args.BusinessCode;
                await _organizationManager.AssignAgent(model);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful"
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


        #endregion AgentMgt


        #region FarmerMgt
        [HttpPost("bulk-onboard")]
        public async Task<IActionResult> BulkOnboard(IFormFile file,[FromQuery] OnBoardingArgs args)
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

                args.OnboardedByUserId = CurrentUserId;
                args.UserId = CurrentUserId;
                NormalizeOrganizationCode(args);
                args.userType = "Farmer";

                if (file == null || file.Length == 0)
                    return BadRequest("Excel file is required.");


                using var stream = file.OpenReadStream();
                var response = await _organizationManager.BulkOnboardFarmersBatchAsync(stream, args);

                return Ok(new Response<BulkOnboardingResult>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = response
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
        [HttpPost("create-membership")]
        
        public async Task<IActionResult> CreateMembershipUser([FromBody] MemberShipRequestModel model, [FromQuery] OnBoardingArgs args)
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
                args.OnboardedByUserId = CurrentUserId;
                NormalizeOrganizationCode(args);
              
                var request = await _organizationManager.CreateMembershipUser(model, args);


                return Ok(new Response<CreateMembershipResponseModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = request.Message,
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
        [HttpGet("farmer/sales-overview")]

        public async Task<IActionResult> GetOrganizationFarmerSalesSummary([FromQuery] ListQueryParams query)
        {
            try
            {
                NormalizeOrganizationCode(new OnBoardingArgs
                {
                    organizationCode = query.organizationCode,
                });
               if(string.IsNullOrEmpty(query.FarmerUserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Farmer user Id is required");
                }
                var overview = await _organizationManager.GetOrganizationFarmerSalesSummary(query);
                return Ok(new Response<OrganizationFarmerSalesSummary>()
                {
                    Code = (overview != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = "Successful",
                    Data = overview
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

        [HttpGet("farmer/sales-history")]

        public async Task<IActionResult> FarmerSalesHistory([FromQuery] ListQueryParams query, CancellationToken cancellationToken)
        {
            try
            {
                NormalizeOrganizationCode(new OnBoardingArgs
                {
                    organizationCode = query.organizationCode,
                });
                if (string.IsNullOrEmpty(query.FarmerUserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Farmer user Id is required");
                }
                var response = await _organizationManager.FarmerSalesHistory(query, cancellationToken);
                return Ok(new PagedListResponse<List<OrganizationFarmerSalesHistor>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = query.PageSize,
                    PageNumber = query.PageNumber
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


        [HttpPut("deactivate-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> DeactivateFarmerFromOrganization([FromQuery] AssignmentArgs args, 
            [FromBody] TaskDescription model)
        {

            try
            {
                
                args.OnboardedByUserId = CurrentUserId;
                NormalizeOrganizationCode(new OnBoardingArgs
                {
                    organizationCode = args.organizationCode,
                });
                await EnrichFarmerAsync(args);
                var request = await _organizationManager.DeactivateFarmerFromOrganization(args, model.Message);


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

        [HttpPut("assign-cooperatives-to-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignFarmerToCooperatives([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
        {

            try
            {
                if (!model.MembershipIds.Any())
                {
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "No Memberhsip selected.");
                }
                args.OnboardedByUserId = CurrentUserId;
                NormalizeOrganizationCode(new OnBoardingArgs
                {
                    organizationCode= args.organizationCode,
                });
                await EnrichFarmerAsync(args);
                args.CooperativeIds = model.MembershipIds;

                var request = await _organizationManager.AssignFarmerToCooperatives(args);


                return Ok(new Response<FarmerModel>()
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
        [HttpPost("create-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateFarmer([FromBody] CreateFarmerRequest model, [FromQuery] OnBoardingArgs args)
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
                args.OnboardedByUserId = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                NormalizeOrganizationCode(args);


                var request = await _organizationManager.CreateFarmerAsync(model, args);


                return Ok(new Response<CreateFarmerResponseModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = request.Message,
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

        [HttpGet("farmer-mgt-overview")]

        public async Task<IActionResult> GetOrganizationFarmerDashboardAsync([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = !string.IsNullOrEmpty(query.organizationCode) ? query.organizationCode : OrganizationCode;

                if (string.IsNullOrEmpty(query.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var overview = await _organizationManager.GetOrganizationFarmerDashboardAsync(query);
                return Ok(new Response<OrganizationFarmerDashboardResponse>()
                {
                    Code = (overview != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = "Successful",
                    Data = overview
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

        [HttpGet("farmers")]
        public async Task<IActionResult> GetOrganizationFarmersAsync([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;



                var result = await _organizationManager.GetOrganizationFarmersAsync(query);
                return Ok(new PagedListResponse<List<OrganizationFarmerResponse>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Data.Count > 0,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = (result.Succeeded && result.Data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpGet("farmer-detail")]
        public async Task<IActionResult> GetFarmerDetailsAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetFarmerDetailsAsync(args);
                return Ok(response);
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

        //bulk farmer upload
        //link cooperative
        //assign agent
        //deactivate farmer
        //list farmer, including the agent and cooperative,
        #endregion FarmerMgt

        private void NormalizeOrganizationCode(OnBoardingArgs args)
        {
            if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
            {
                args.organizationCode = OrganizationCode;
            }
            if (string.IsNullOrEmpty(args.organizationCode))
            {
                throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
            }
        }

        private async Task EnrichCooperativeAsync(OnBoardingArgs args)
        {
            //  if (CurrentUserRole != args.userType) return;
            if (args.Id != 0) return;

            var coop = await _businessManager.GetCooperativeDetailByUserId(args.MembershipUserId);

            if (coop == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Cooperative not found for the specified user.");

            args.Id = coop.Id;

            args.MembershipIds ??= new List<long?>();

            if (!args.MembershipIds.Contains(coop.Id))
                args.MembershipIds.Add(coop.Id);
        }
        private async Task EnrichAgentAsync(OnBoardingArgs args)
        {
            //  if(CurrentUserRole != args.userType) return;
            if (args.Id != 0) return;

            var agent = await _businessManager.GetAgentDetailByUserId(args.AgentUserId);

            if (agent == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Agent not found for the specified user.");

            args.Id = agent.Id;

            args.AgentIds ??= new List<long?>();

            if (!args.AgentIds.Contains(agent.Id))
                args.AgentIds.Add(agent.Id);
        }
        private async Task EnrichFarmerAsync(AssignmentArgs args)
        {
            if (args.Id != 0) return;
            if(string.IsNullOrEmpty(args.farmerUserId)) throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "Farmer Id is required.");
            var farmer = await _businessManager.GetFarmerByUserId(args.farmerUserId);

            if (farmer == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Farmer not found for the specified user.");

            args.Id = farmer.Id;

        }

    }
}
