using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using CsvHelper;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Claims;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/agent")]
    [ApiController]
  //  [Authorize]
    public class AgentController : BaseController
    {
        private readonly IAgentManager _agentManager;
        private readonly IBusinessManager _businessManager;
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly IBulkFarmerUploadService _bulkFarmerUploadService;
        private readonly IMembershipManager _membershipManager;
        private readonly IBulkMembershipExcelParser _parser;
        private readonly IBulkMembershipValidator _validator;
        private readonly IBulkMembershipUploadService _memberShipUploadService;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        public AgentController(IHttpContextAccessor httpContextAccessor, IAgentManager agentManager,
            IBusinessManager businessManager,
            IOnBoardingManager onBoardingManager,
            IMembershipManager membershipManager,

                IBulkFarmerUploadService bulkFarmerUploadService,
        IBulkMembershipExcelParser parser,
        IBulkMembershipValidator validator,
        IBulkMembershipUploadService memberShipUploadService) : base(httpContextAccessor)
        {
           _agentManager = agentManager;
            _onBoardingManager = onBoardingManager;
            _businessManager = businessManager;
            _bulkFarmerUploadService = bulkFarmerUploadService;
            _parser = parser;
            _validator = validator;
            _memberShipUploadService = memberShipUploadService;
            _membershipManager = membershipManager;
        }

        [HttpPost("bulk-upload")]
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
            await EnrichAgentAsync(args);

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

            // ── Process ───────────────────────────────────────────────────
            var uploadedBy = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;


            var result = await _bulkFarmerUploadService.ProcessAsync(rows, args);

            // 207 Multi-Status when at least one row failed, 200 when all succeeded
            int statusCode = result.Failed > 0 ? StatusCodes.Status207MultiStatus
                                               : StatusCodes.Status200OK;

            return StatusCode(statusCode, result);
        }


        [HttpPost("bulk-upload-membership")]       
        [RequestSizeLimit(MaxFileSizeBytes)] // 10 MB cap
        public async Task<IActionResult> BulkMembershipUpload(
        IFormFile file,
        [FromQuery] OnBoardingArgs args)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            var onboardedById = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
            args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;
            await EnrichAgentAsync(args);
            // 1. Parse
            using var stream = file.OpenReadStream();
            List<BulkMembershipRowDto> rows;
            try
            {
                rows = _parser.Parse(
                    stream, args.businessCode, onboardedById,
                    args.organizationCode, args.Id);
            }
            catch (Exception ex)
            {
                return BadRequest($"Could not parse file: {ex.Message}");
            }

            if (!rows.Any())
                return BadRequest("The file contains no data rows.");

            // 2. Pre-validate ALL rows — zero DB writes until this passes
            var validationErrors = await _validator.ValidateAllAsync(rows,args);
            if (validationErrors.Any())
            {
                return UnprocessableEntity(new
                {
                    Message = "Validation failed. No records were saved.",
                    Errors = validationErrors
                });
            }

            // 3. Process (chunked transactions)
            var result = await _memberShipUploadService.ProcessBulkMembershipUploadAsync(rows, args);

            return result.FailureCount == 0
                ? Ok(result)
                : StatusCode(207, result); // 207 Multi-Status: partial success
        }
       
        
        [HttpPatch("assign/memberhip-leader")]
        public async Task<IActionResult> AssignLeader(
        [FromQuery] OnBoardingArgs args,
        [FromBody] AssignLeaderDto dto)
        {
            if (args.Id == 0) return BadRequest("Invalid Membership selected");
            dto.MembershipId = args.Id; // bind from route
            args.UserId = CurrentUserId;
            var result = await _membershipManager.AssignLeaderAsync(dto, args);
            return Ok(result);
        }

        [HttpPost("add-membership")]
        public async Task<IActionResult> Create([FromBody] CreateMembershipDto dto,
            [FromQuery] OnBoardingArgs args)
        {
            args.UserId = CurrentUserId;
            args.organizationCode = OrganizationCode;
            
            await EnrichAgentAsync(args);
            var result = await _membershipManager.CreateMembershipAsync(dto, args);
            return Ok(result);
         }
        //      [HttpPost("bulk-onboard")]
        //      public async Task<IActionResult> BulkOnboard(
        //IFormFile file,
        //[FromQuery] OnBoardingArgs args)
        //      {
        //          if (!ModelState.IsValid)
        //          {

        //              return BadRequest(new Response<Dictionary<string, string>>()
        //              {
        //                  Message = "Validation failed.",
        //                  Succeeded = false,
        //                  Data = ModelState
        //                      .Where(ms => ms.Value.Errors.Any())
        //                      .ToDictionary(
        //                          ms => ms.Key,
        //                          ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
        //                      )
        //              });
        //          }
        //          try
        //          {

        //              args.OnboardedByUserId = CurrentUserId;
        //              args.UserId = CurrentUserId;
        //              NormalizeOrganizationCode(args);
        //              args.userType = "Farmer";
        //              await EnrichAgentAsync(args);

        //              if (file == null || file.Length == 0)
        //                  return BadRequest("Excel file is required.");


        //              using var stream = file.OpenReadStream();
        //             // var response = await _onBoardingManager.BulkOnboardFarmers(stream, args);
        //              var response = await _onBoardingManager.BulkOnboardFarmersBatchAsync(stream, args);

        //              return Ok(new Response<BulkOnboardingQueueResponse>()
        //              {
        //                  Code = ResponseCodes.Success,
        //                  Succeeded = true,
        //                  Message = "Successful",
        //                  Data = response
        //              });


        //          }
        //          catch (BaseException ex)
        //          {
        //              return StatusCode(400, new Response<string>
        //              {
        //                  Message = ex.ExceptionMessage,
        //                  Succeeded = false,

        //              });
        //          }
        //          catch (Exception ex)
        //          {
        //              return StatusCode(400, new Response<string>
        //              {
        //                  Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
        //                  Succeeded = false,
        //              });
        //          }

        //      }

        [HttpGet("memberships")]
        public async Task<IActionResult> GetCooperativesByAgent([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                if (!await _onBoardingManager.IsUserInRoleAsync(query.UserId, "Agent"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a Agent.",
                        Succeeded = false
                    });
                }
                var result = await _agentManager.GetMembershipsByAgent(query.UserId, query);
                return Ok(new PagedListResponse<List<MemberShipResponseModel>>()
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
                //  ResolveUserContext(args);
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

                if (!await _onBoardingManager.IsUserInRoleAsync(args.UserId, "Agent"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a Agent.",
                        Succeeded = false
                    });
                }
                await EnrichAgentAsync(args);
              

                var request = await _onBoardingManager.CreateMembershipUser(model, args);


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
        public async Task<IActionResult> GetFarmersByAgent([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                if (!await _onBoardingManager.IsUserInRoleAsync(query.UserId, "Agent"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a Agent.",
                        Succeeded = false
                    });
                }
                var result = await _agentManager.GetFarmersByAgent(query.UserId, query);
                return Ok(new PagedListResponse<List<FarmerAgentDto>>()
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
        
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetAgentDashboardData([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                if (!await _onBoardingManager.IsUserInRoleAsync(query.UserId, "Agent"))
                {
                    return StatusCode(400, new Response<string>
                    {
                        Message = "The supplied user is not a Agent.",
                        Succeeded = false
                    });
                }
                var result = await _agentManager.GetAgentDashboardData(query);
                return Ok(new Response<AgentDashboardDto>()
                {
                    Data = result,

                   
                    Message = result !=null? "Successful": "Could not retrieve data",
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
        [HttpPost("update-profile")]
        [AllowAnonymous]
        public async Task<IActionResult> EditAgentUser([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
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
                args.UserId = args.UserId;

                args.organizationCode = args.organizationCode ?? OrganizationCode;


                var request = await _onBoardingManager.EditAgentUser(model, args);


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


        private async Task EnrichArgsByRoleAsync(OnBoardingArgs args)
        {
            var currentRole = CurrentUserRole;

            switch (currentRole)
            {
                case "MemberShip":
                    await EnrichCooperativeAsync(args);
                    break;

                case "Agent":
                    await EnrichAgentAsync(args);
                    break;
            }
        }
        private async Task EnrichCooperativeAsync(OnBoardingArgs args)
        {
            //  if (CurrentUserRole != args.userType) return;
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
        private async Task EnrichAgentAsync(OnBoardingArgs args)
        {
            //  if(CurrentUserRole != args.userType) return;
            if (args.Id != 0) return;
            args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
            var agent = await _businessManager.GetAgentDetailByUserId(args.UserId);

            if (agent == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Agent not found for the specified user.");

            args.Id = agent.Id;

            args.AgentIds ??= new List<long?>();

            if (!args.AgentIds.Contains(agent.Id))
                args.AgentIds.Add(agent.Id);
        }
        private void NormalizeOrganizationCode(OnBoardingArgs args)
        {
            if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
            {
                args.organizationCode = OrganizationCode;
            }
        }
        private void ResolveUserContext(OnBoardingArgs args)
        {
            var currentRole = CurrentUserRole;

            if (currentRole == "Farmer")
            {
                args.UserId ??= CurrentUserId;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(args.UserId))
                {
                    throw new BaseException(
                        ExceptionType.BAD_REQUEST,
                        "UserId must be provided to create a profile for the farmer."
                    );
                }
            }
        }

    }
}
