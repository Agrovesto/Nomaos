using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/memberships")]
    [ApiController]
    public class MembershipController : BaseController
    {

        private static readonly string[] AllowedExtensions = { ".csv", ".xlsx", ".xls" };
        private readonly IMembershipManager _membershipManager;
        private readonly IBusinessManager _businessManager;
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly ILogger<MembershipController> _logger;
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private readonly IBulkFarmerUploadService _bulkFarmerUploadService;
        public MembershipController(IHttpContextAccessor httpContextAccessor,
            IOnBoardingManager onBoardingManager,
            ILogger<MembershipController> logger,

                IBulkFarmerUploadService bulkFarmerUploadService,
            IMembershipManager membershipManager, IBusinessManager businessManager) : base(httpContextAccessor)
        {
            _onBoardingManager=onBoardingManager;
            _membershipManager = membershipManager;
            _businessManager = businessManager;
            _logger = logger;
            _bulkFarmerUploadService = bulkFarmerUploadService;
        }


        [HttpPost("bulk-onboard")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> BulkUpload(
    IFormFile file,
    [FromQuery] OnBoardingArgs args)
        {
            if (file is null || file.Length == 0)
                return Error("FILE_MISSING", "Please choose a file to upload before submitting.");

            if (file.Length > MaxFileSizeBytes)
                return Error("FILE_TOO_LARGE",
                    $"That file is too large. Please upload a file smaller than {MaxFileSizeBytes / (1024 * 1024)} MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return Error("FILE_TYPE_INVALID",
                    "We couldn't read that file. Please upload a CSV or Excel file (.csv, .xlsx, .xls).");

            if (string.IsNullOrWhiteSpace(args.businessCode))
                return Error("BUSINESS_CODE_REQUIRED", "A business code is required to upload farmers.");

            args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.UserId))
                return Error("UNAUTHORIZED", "Your session has expired. Please sign in again and retry.",
                    StatusCodes.Status401Unauthorized);

            args.organizationCode = OrganizationCode;
              await EnrichCooperativeAsync(args);
            List<BulkFarmerRowDto> rows;
            try
            {
                await using var stream = file.OpenReadStream();
                rows = BulkFarmerCsvParser.Parse(stream, file.FileName);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogWarning(ex, "Unsupported file type uploaded: {FileName}", file.FileName);
                return Error("FILE_TYPE_INVALID", "We couldn't read that file format. Please upload a CSV or Excel file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse bulk upload file: {FileName}", file.FileName);
                return Error("FILE_PARSE_ERROR",
                    "We couldn't read this file. Please check that it's a valid CSV/Excel export and try again.");
            }

            if (rows.Count == 0)
                return Error("FILE_EMPTY", "This file doesn't contain any farmer records to upload.");

            try
            {
                var result = await _bulkFarmerUploadService.ProcessAsync(rows, args);

                int statusCode = result.Failed > 0
                    ? StatusCodes.Status207MultiStatus
                    : StatusCodes.Status200OK;

                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk farmer upload processing failed for business {BusinessCode}", args.businessCode);
                return Error("UPLOAD_PROCESSING_FAILED",
                    "Something went wrong while processing your upload. Please try again, and contact support if the problem continues.",
                    StatusCodes.Status500InternalServerError);
            }
        }
       
        

        [HttpPut("update-farmer-profile")]
        public async Task<IActionResult> UpdateFarmerProfile([FromBody] MemberShipModel model, [FromQuery] string? userId)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
            

                var targetUserId = !string.IsNullOrEmpty(userId) ? userId : CurrentUserId;

                if (!await _onBoardingManager.IsUserInRoleAsync(targetUserId, "MemberShip"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a Membership user.",
                        Succeeded = false
                    });
                }
                var result = await _onBoardingManager.UpdateMembershipProfile(targetUserId, model);

                return Ok(new Response<MemberShipUpdateResultDto>()
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

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetMembershipDashboardData([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
              
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                if (!await _onBoardingManager.IsUserInRoleAsync(query.UserId, "MemberShip"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a MemberShip.",
                        Succeeded = false
                    });
                }

                var result = await _membershipManager.GetMembershipDashboardAsync(query);
                return Ok(new Response<MembershipDashboardDto>()
                {
                    Data = result,

                    Message = result != null ? ResponseMessages.Successful : ResponseMessages.NotFound,
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

        [HttpPost("create-farmer")]
        public async Task<IActionResult> CreateFarmer([FromBody] CreateFarmerRequest model, [FromQuery] OnBoardingArgs args)
        {

            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
                args.OnboardedByUserId = CurrentUserId;
                args.UserId = CurrentUserId;
                if (!await _onBoardingManager.IsUserInRoleAsync(args.UserId, "MemberShip"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a MemberShip.",
                        Succeeded = false
                    });
                }

                NormalizeOrganizationCode(args);
               
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

        [HttpGet("farmers")]
        public async Task<IActionResult> GetFarmersByCooperativeId([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                if (!await _onBoardingManager.IsUserInRoleAsync(query.UserId, "MemberShip"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a MemberShip.",
                        Succeeded = false
                    });
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

        [HttpGet("agents")]
        public async Task<IActionResult> GetAgentsByMembership([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }

                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                if (!await _onBoardingManager.IsUserInRoleAsync(query.UserId, "MemberShip"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a MemberShip.",
                        Succeeded = false
                    });
                }

                var result = await _membershipManager.GetAgentsByMembership(query);
                return Ok(new PagedListResponse<List<AgentMembershipDto>>()
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
       
        
        [HttpGet("farmer-details")]
        public async Task<IActionResult> GetFarmerDetails([FromQuery] string? userId)
        {
            try
            {

                userId = !string.IsNullOrEmpty(userId) ? userId : CurrentUserId;


                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new Response<string>
                    {
                        Message = "User Id is required.",
                        Succeeded = false
                    });
                }
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
        
        [HttpGet("agent-details")]
        public async Task<IActionResult> AgentDetails([FromQuery] ListQueryParams args)
        {
            try
            { 

                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<string>
                    {
                        Message = "User Id is required.",
                        Succeeded = false
                    });
                }

                if (!await _onBoardingManager.IsUserInRoleAsync(args.UserId, "Agent"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not an Agent.",
                        Succeeded = false
                    });
                }
                await GetAgentIdFromUserIdAsync(args);

                var profile = await _membershipManager.GetAgentOverview(args);

                return Ok(new Response<AgentOverviewDto>()
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
        private async Task GetAgentIdFromUserIdAsync(ListQueryParams args)
        {
           
            if (args.Id != 0) return;

            var agent = await _businessManager.GetAgentDetailByUserId(args.UserId);

            if (agent == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Agent not found for the specified user.");

            args.Id = agent.Id;

         
        }

        [HttpPost("update-membership-profile")]
        public async Task<IActionResult> UpdateMembershipUser([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;
                args.OnboardedByUserId = CurrentUserId;
                NormalizeOrganizationCode(args);
                if (!await _onBoardingManager.IsUserInRoleAsync(args.UserId, "MemberShip"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a MemberShip.",
                        Succeeded = false
                    });
                }
                model.Id = args.UserId;
                 await _onBoardingManager.CreateCooperativeProfileAsync(model, args);


                return Ok(new Response<CreateMembershipResponseModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = null
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

        [HttpPost("create-agent")]
        public async Task<IActionResult> CreateAgentUser([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
                args.OnboardedByUserId = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                NormalizeOrganizationCode(args);
                if (!await _onBoardingManager.IsUserInRoleAsync(args.UserId, "MemberShip"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a MemberShip.",
                        Succeeded = false
                    });
                }
                await EnrichCooperativeAsync(args);


                var request = await _onBoardingManager.CreateAgentUser(model, args);


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
        [HttpGet("agent-farmers")]
        public async Task<IActionResult> GetFarmersByAgent([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                if (string.IsNullOrEmpty(query.UserId))
                {
                    return BadRequest(new Response<string>
                    {
                        Message = "User Id is required.",
                        Succeeded = false
                    });
                }
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }

                if (!await _onBoardingManager.IsUserInRoleAsync(query.UserId, "Agent"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a Agent.",
                        Succeeded = false
                    });
                }
                var result = await _membershipManager.GetFarmersByAgent(query.UserId, query);
                return Ok(new PagedListResponse<List<FarmerAgentDto>>()
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
       
        [HttpPut("assign-agent-to-farmer")]
        public async Task<IActionResult> AssignFarmerToAgents([FromQuery] AssignmentArgs args)
        {

            try
            {
                args.OnboardedByUserId = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                
                await EnrichAgentAsync(args);
                await EnrichFarmerAsync(args);
                var request = await _onBoardingManager.AssignFarmerToAgents(args);


                return Ok(new Response<FarmerModel>()
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


        private void NormalizeOrganizationCode(OnBoardingArgs args)
        {
            if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
            {
                args.organizationCode = OrganizationCode;
            }
        }
        private async Task EnrichCooperativeAsync(OnBoardingArgs args)
        {
            if (args.Id != 0) return;

            var coop = await _businessManager.GetCooperativeDetailByUserId(CurrentUserId);

            if (coop == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Cooperative not found for the specified user.");

            args.Id = coop.Id;

            args.CooperativeIds ??= new List<long?>();

            if (!args.CooperativeIds.Contains(coop.Id))
                args.CooperativeIds.Add(coop.Id);
        }
        private async Task EnrichAgentAsync(AssignmentArgs args)
        {
            if (args.Id != 0) return;

            var agent = await _businessManager.GetAgentDetailByUserId(args.agentUserId);

            if (agent == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Agent not found for the specified user.");


            args.AgentIds ??= new List<long?>();

            if (!args.AgentIds.Contains(agent.Id))
                args.AgentIds.Add(agent.Id);
        }

        private async Task EnrichFarmerAsync(AssignmentArgs args)
        {
            if (args.Id != 0) return;

            var farmer = await _businessManager.GetFarmerByUserId(args.farmerUserId);

            if (farmer == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Agent not found for the specified user.");

            args.Id = farmer.Id;

        }
    }
}
