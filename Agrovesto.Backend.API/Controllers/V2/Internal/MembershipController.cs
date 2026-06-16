using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Services;
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

        private readonly IMembershipManager _membershipManager;
        private readonly IBusinessManager _businessManager;
        private readonly IOnBoardingManager _onBoardingManager;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private readonly IBulkFarmerUploadService _bulkFarmerUploadService;
        public MembershipController(IHttpContextAccessor httpContextAccessor,
            IOnBoardingManager onBoardingManager,

                IBulkFarmerUploadService bulkFarmerUploadService,
            IMembershipManager membershipManager, IBusinessManager businessManager) : base(httpContextAccessor)
        {
            _onBoardingManager=onBoardingManager;
            _membershipManager = membershipManager;
            _businessManager = businessManager;

            _bulkFarmerUploadService = bulkFarmerUploadService;
        }


        [HttpPost("bulk-onboard")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> BulkUpload(
            IFormFile file,
            [FromQuery] OnBoardingArgs args)
        {
            // ── Basic input checks ────────────────────────────────────────
            if (file is null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only CSV files are accepted.");

            if (string.IsNullOrWhiteSpace(args.businessCode))
                return BadRequest("businessCode is required.");

            args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.UserId))
                return Unauthorized("Access Denied");
            args.organizationCode = OrganizationCode;
            await EnrichCooperativeAsync(args);


            List<BulkFarmerRowDto> rows;
            try
            {
                await using var stream = file.OpenReadStream();
                rows = BulkFarmerCsvParser.Parse(stream);
            }
            catch (Exception ex)
            {
                return BadRequest($"Could not parse CSV: {ex.Message}");
            }

            if (rows.Count == 0)
                return BadRequest("The uploaded file contains no data rows.");

            var uploadedBy = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;


            var result = await _bulkFarmerUploadService.ProcessAsync(rows, args);

            int statusCode = result.Failed > 0 ? StatusCodes.Status207MultiStatus
                                               : StatusCodes.Status200OK;

            return StatusCode(statusCode, result);
        }


        [HttpPut("update-farmer-profile")]
        public async Task<IActionResult> UpdateFarmerProfile([FromBody] MemberShipModel model, [FromQuery] string? userId)
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


      /**  [HttpPost("bulk-onboard")]
        public async Task<IActionResult> BulkOnboard(
        IFormFile file,
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

                args.OnboardedByUserId = CurrentUserId;
                args.UserId = CurrentUserId;
                NormalizeOrganizationCode(args);
                args.userType = "Farmer";
                await EnrichCooperativeAsync(args);

                if (file == null || file.Length == 0)
                    return BadRequest("Excel file is required.");


                using var stream = file.OpenReadStream();
               // var response = await _onBoardingManager.BulkOnboardFarmers(stream, args);
                var response = await _onBoardingManager.BulkOnboardFarmersBatchAsync(stream, args);

                return Ok(new Response<BulkOnboardingQueueResponse>()
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
        **/


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

        [HttpPost("create-farmer")]
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
        private async Task GetAgentIdFromUserIdAsync(ListQueryParams args)
        {
            //  if(CurrentUserRole != args.userType) return;
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
                    Message = "Successful",
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
                if (!await _onBoardingManager.IsUserInRoleAsync(args.UserId, "MemberShip"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a MemberShip.",
                        Succeeded = false
                    });
                }
                //  ResolveUserContext(args);
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
