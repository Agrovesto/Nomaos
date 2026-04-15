using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/organization")]
    [ApiController]
    public class OrganizationController : BaseController
    {

        private readonly IBusinessManager _businessManager;
        private readonly IOrganizationManager _organizationManager;
        private readonly IOnBoardingManager _onboardingManager;
        private readonly Agrovesto.Backend.Infrastructure.DB.DataContext _context; 
        private readonly IMembershipManager _membershipManager;

        private readonly IFarmMappingManager _farmMappingManager;
        public OrganizationController(IHttpContextAccessor httpContextAccessor,
            IBusinessManager businessManager,
             IOrganizationManager organizationManager,
             IMembershipManager membershipManager,
             IOnBoardingManager onboardingManager, IFarmMappingManager farmMappingManager) : base(httpContextAccessor)
        {
            _farmMappingManager = farmMappingManager;
            _organizationManager = organizationManager;
            _businessManager = businessManager;
            _onboardingManager = onboardingManager;
            _membershipManager = membershipManager;
            _context = (Agrovesto.Backend.Infrastructure.DB.DataContext)httpContextAccessor.HttpContext.RequestServices.GetService(typeof(Agrovesto.Backend.Infrastructure.DB.DataContext));
        }

        [HttpGet("bulk-jobs")]
        public async Task<IActionResult> ListBulkJobs([FromQuery] string organizationCode = null, [FromQuery] string status = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.BulkUploadJobs.AsQueryable();

                if (!string.IsNullOrWhiteSpace(organizationCode))
                    query = query.Where(j => j.OrganizationCode == organizationCode);

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(j => j.Status == status);

                var total = await query.CountAsync();

                var items = await query
                    .OrderByDescending(j => j.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(j => new
                    {
                        j.Id,
                        j.OrganizationCode,
                        j.Status,
                        j.ErrorMessage,
                        j.CreatedAt,
                        j.CompletedAt,
                        j.TotalRows,
                        j.Successful,
                        j.Failed
                    })
                    .ToListAsync();

                return Ok(new PagedListResponse<List<object>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = items.Cast<object>().ToList(),
                    Count = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = ex.Message });
            }
        }

        [HttpGet("bulk-jobs/{id}/file")]
        public async Task<IActionResult> DownloadBulkJobFile(Guid id)
        {
            try
            {
                var job = await _context.BulkUploadJobs.FindAsync(id);
                if (job == null)
                    return NotFound(new Response<string> { Succeeded = false, Message = "Job not found" });

                if (string.IsNullOrWhiteSpace(job.FilePath) || !System.IO.File.Exists(job.FilePath))
                    return NotFound(new Response<string> { Succeeded = false, Message = "Uploaded file not found" });

                var fs = System.IO.File.OpenRead(job.FilePath);
                var fileName = Path.GetFileName(job.FilePath);
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(fs, contentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = ex.Message });
            }
        }
        [HttpGet("bulk-jobs/{id}")]
        public async Task<IActionResult> GetBulkJob(Guid id)
        {
            try
            {
                var job = await _context.BulkUploadJobs.FindAsync(id);
                if (job == null)
                    return NotFound(new Response<string> { Succeeded = false, Message = "Job not found" });

                return Ok(new Response<object>
                {
                    Succeeded = true,
                    Data = new
                    {
                        job.Id,
                        job.FilePath,
                        job.OrganizationCode,
                        job.Status,
                        job.ErrorMessage,
                        job.CreatedAt,
                        job.CompletedAt,
                        job.TotalRows,
                        job.Successful,
                        job.Failed
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = ex.Message });
            }
        }

        #region settings
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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

                args.organizationCode = args.organizationCode ?? OrganizationCode;

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

                args.organizationCode = args.organizationCode ?? OrganizationCode;

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
                query.organizationCode = query.organizationCode ?? OrganizationCode;
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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

                args.organizationCode = args.organizationCode ?? OrganizationCode;
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

                args.organizationCode = args.organizationCode ?? OrganizationCode;
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
                query.organizationCode = query.organizationCode ?? OrganizationCode;
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



        #endregion settings
        #region AgentMgt

        [HttpGet("agent-mgt-overview")]

        public async Task<IActionResult> GetAgentManagementOverview([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
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
        [HttpGet("agents")]
        public async Task<IActionResult> GetOrganizationAgents([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
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
                    PageSize = query.PageSize,
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;


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
          [HttpGet("agent-detail")]
        public async Task<IActionResult> GetAgentDetail([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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
                args.OrganizationCode = args.OrganizationCode ?? OrganizationCode;
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
                args.OrganizationCode = args.OrganizationCode ?? OrganizationCode;
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
                args.OrganizationCode = args.OrganizationCode ?? OrganizationCode;
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
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

                args.OrganizationCode = args.OrganizationCode ?? OrganizationCode;
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

        #region Membership

        [HttpGet("membership-mgt-overview")]

        public async Task<IActionResult> GetOrganizationMembershipDashboard([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var overview = await _organizationManager.GetOrganizationMembershipDashboard(query);
                return Ok(new Response<OrganizationMembershipDashboardResponse>()
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;

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

        [HttpGet("memberships")]
        public async Task<IActionResult> GetCooperativesByAgent([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var result = await _organizationManager.GetMembershipsByOrganization(query);
                return Ok(new PagedListResponse<List<OrganizationMemberShipResponseModel>>()
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

        [HttpGet("membership/farmers")]
        public async Task<IActionResult> GetFarmersByCooperativeId([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;

                var currentRole = CurrentUserRole;
                if (string.IsNullOrEmpty(query.UserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "userId is required");
                }
                var result = await _membershipManager.GetMembershipFarmers(query);
                return Ok(new PagedListResponse<List<FarmerMembershipDto>>()
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
        [HttpPost("membership/create-farmer")]
        public async Task<IActionResult> CreateMembershipFarmer([FromBody] CreateFarmerRequest model, [FromQuery] OnBoardingArgs args)
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
                if (string.IsNullOrEmpty(args.UserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Membership userId is required");
                }
                args.OnboardedByUserId = CurrentUserId;

                args.organizationCode = args.organizationCode ?? OrganizationCode;


                await EnrichCooperativeAsync(args);


                var request = await _membershipManager.CreateFarmerAsync(model, args);


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

        [HttpGet("membership-detail")]
        public async Task<IActionResult> GetMembershipDashboardData([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.UserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Membership userId is required");
                }
                var result = await _membershipManager.GetMembershipDashboardAsync(query);
                return Ok(new Response<MembershipDashboardDto>()
                {
                    Data = result,


                    Message = result != null ? "Successful" : "Could not retrieve data",
                    Succeeded = result != null ? true : false,

                    Code = result != null ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        #endregion Membership

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

                args.organizationCode = args.organizationCode ?? OrganizationCode;

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
           [HttpGet("farmer/sales-overview")]

        public async Task<IActionResult> GetOrganizationFarmerSalesSummary([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.FarmerUserId))
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
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.FarmerUserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Farmer user Id is required");
                }
                var response = await _organizationManager.FarmerSalesHistory(query, cancellationToken);
                return Ok(new PagedListResponse<List<OrganizationFarmerSalesHistor>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response.Count > 0 ? "Record Retrieved" : "No Records Found",
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

        [HttpGet("farmer/input-payment-overview")]
        public async Task<IActionResult> GetFinancingRepaymentSummaryAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var response = await _organizationManager.GetFinancingRepaymentSummaryAsync(args);
                return Ok(new Response<FinancingRepaymentSummaryDto>()
                {
                    Code = (response != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = response != null ? "Record Retrieved" : "No Records Found",
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

        [HttpGet("farmer/input-payment-history")]
        public async Task<IActionResult> GetRepaymentBreakdownAsync([FromQuery] ListQueryParams args, CancellationToken cancellationToken)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var response = await _organizationManager.GetRepaymentBreakdownAsync(args.Id, args, cancellationToken);
                return Ok(new PagedListResponse<List<RepaymentBreakdownDto>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response.Count > 0 ? "Record Retrieved" : "No Records Found",
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

        [HttpGet("farmer/input-financing-detail")]
        public async Task<IActionResult> GetLoanDetailAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
      
                var response = await _organizationManager.GetLoanDetailAsync(args);
                return Ok(new Response<LoanDetailDto>()
                {
                    Code = (response != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = response != null ? "Record Retrieved" : "No Records Found",
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

        [HttpGet("farmer/input-financing-overview")]

        public async Task<IActionResult> GetOrganizationFarmerSummary([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                var overview = await _organizationManager.GetInputLoanSummaryAsync(query);
                return Ok(new Response<InputLoanSummaryDto>()
                {
                    Code = (overview != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = overview != null ? "Record Retrieved" : "No Records Found",
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

        [HttpGet("farmer/input-financing-history")]

        public async Task<IActionResult> GetFinancingBreakdownAsync([FromQuery] ListQueryParams query, CancellationToken cancellationToken)
        {
            try
            {

                query.organizationCode = query.organizationCode ?? OrganizationCode;

                var response = await _organizationManager.GetFinancingBreakdownAsync(query, cancellationToken);
                return Ok(new PagedListResponse<List<FinancingBreakdownDto>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response.Count > 0 ? "Record Retrieved" : "No Records Found",
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


        //deactivate farmer
        [HttpPut("deactivate-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> DeactivateFarmerFromOrganization([FromQuery] AssignmentArgs args, 
            [FromBody] TaskDescription model)
        {

            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                args.OnboardedByUserId = CurrentUserId;
               
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
      
        //link cooperative
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

                args.organizationCode = args.organizationCode ?? OrganizationCode;
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
        [HttpPut("remove-farmer-from-cooperative")]
        public async Task<IActionResult> RemoveFarmerFromCooperative([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
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
                if (!model.MembershipIds.Any())
                {
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "No Memberhsip selected.");
                }
                args.OnboardedByUserId = CurrentUserId;

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                await EnrichFarmerAsync(args);
                args.CooperativeIds = model.MembershipIds;
                var CooperativeIds = args.CooperativeIds
  .Where(x => x.HasValue)
  .Select(x => x.Value)
  .ToList();
                var updateDetail = await _organizationManager.RemoveFarmerFromCooperatives(args.farmerUserId, CooperativeIds);


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

                args.organizationCode = args.organizationCode ?? OrganizationCode;


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

                query.organizationCode = query.organizationCode ?? OrganizationCode;
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

                query.organizationCode = query.organizationCode ?? OrganizationCode;
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
                    PageSize = query.PageSize,
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

                args.organizationCode = args.organizationCode ?? OrganizationCode;
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

        [HttpPost("farmer-face-capture")]
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
               if(string.IsNullOrEmpty(args.UserId))
                {
                    throw new ValidationException("UserId is required");
                }


                args.organizationCode = args.organizationCode ?? OrganizationCode;
                var saveFarmerFace = await _onboardingManager.SaveFarmerImage(model, args);


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

        //bulk farmer upload

        //assign agent
        [HttpPut("change-farmer-agents")]
        public async Task<IActionResult> ChangeFarmerAgents(
       [FromQuery] AssignmentArgs args,
        [FromBody] AssignmentArgs model)
        {
            try {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                await EnrichFarmerAsync(args);
            args.AgentIds = model.AgentIds;

            if (model == null || model.AgentIds == null || !model.AgentIds.Any())
                return BadRequest("AgentIds are required");

            var result = await _onboardingManager.ChangeFarmerAgents(args, args.Id);

            if (result == null)
                return NotFound("Farmer not found");

            
                return Ok(new Response<FarmerModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Farmer agents updated successfully",
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
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }
        [HttpPut("assign-agent-to-farmer")]
        public async Task<IActionResult> AssignFarmerToAgents([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
        {

            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                await EnrichFarmerAsync(args);
                args.AgentIds = model.AgentIds;
                var request = await _onboardingManager.AssignFarmerToAgents(args);


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

        [HttpPut("remove-farmer-from-agent")]
        public async Task<IActionResult> RemoveFarmerToAgents([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
        {

            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                await EnrichFarmerAsync(args);
                args.AgentIds = model.AgentIds;
                var request = await _onboardingManager.RemoveFarmerToAgents(args);


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

        //list farmer, including the agent and cooperative,
        [HttpGet("farmer/farms")]
        public async Task<IActionResult> FetchFarmsByFarmer([FromQuery] FarmMappingArgs query)
        {
            try
            {

                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.farmerUserId)) throw new BaseException(
                            Core.Enums.ExceptionType.BAD_REQUEST,
                            "Farmer Id is required.");
                var farmer = await _businessManager.GetFarmerByUserId(query.farmerUserId);

                if (farmer == null)
                    throw new BaseException(
                        ExceptionType.NOTFOUND,
                        "Farmer not found for the specified user.");

                query.farmerId = farmer.Id;

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

        [HttpGet("farmer/farm-details")]
        public async Task<IActionResult> FetchFarmDetails([FromQuery] FarmMappingArgs query)
        {
            try
            {

                query.organizationCode = query.organizationCode ?? OrganizationCode;
                var currentRole = CurrentUserRole;
                if (query.farmId <= 0)
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
            if (args.Id != 0) return;

            var coop = await _businessManager.GetCooperativeDetailByUserId(args.UserId);

            if (coop == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Cooperative not found for the specified user.");

            args.Id = coop.Id;

            args.CooperativeIds ??= new List<long?>();

            if (!args.CooperativeIds.Contains(coop.Id))
                args.CooperativeIds.Add(coop.Id);


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
