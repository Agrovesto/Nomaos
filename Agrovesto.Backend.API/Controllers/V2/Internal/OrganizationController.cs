using Agrovesto.Backend.Core.CoreUtils;
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

        #region AgentMgt
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
        #endregion AgentMgt
    }
}
