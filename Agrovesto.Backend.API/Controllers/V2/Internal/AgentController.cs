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
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<AgentController> _logger;

        
        public AgentController(IHttpContextAccessor httpContextAccessor, IAgentManager agentManager,
            IBusinessManager businessManager,
            IOnBoardingManager onBoardingManager,
            IMembershipManager membershipManager,
            ILogger<AgentController> logger,
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
            _logger = logger;
        }
      
        
        [HttpPost("bulk-upload")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> BulkUpload(IFormFile file, [FromQuery] OnBoardingArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.businessCode))
                return Error("BUSINESS_CODE_REQUIRED", "A business code is required to upload farmers.");

            args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.UserId))
                return Error("UNAUTHORIZED", "Your session has expired. Please sign in again and retry.",
                    StatusCodes.Status401Unauthorized);

            args.organizationCode = OrganizationCode;
            await EnrichAgentAsync(args);

            return await HandleBulkFarmerUploadAsync(file, args, _bulkFarmerUploadService, _logger);
        }


        [HttpPost("bulk-upload-membership")]
        [RequestSizeLimit(MaxFileSizeBytes)] // 10 MB cap
        public async Task<IActionResult> BulkMembershipUpload(
        IFormFile file,
        [FromQuery] OnBoardingArgs args)
        {
            if (ValidateUploadFile(file) is { } fileError) return fileError;
            if (string.IsNullOrWhiteSpace(args.businessCode))
                return Error("BUSINESS_CODE_REQUIRED", "A business code is required to upload memberships.");

            var onboardedById = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
            if (string.IsNullOrWhiteSpace(onboardedById))
                return Error("UNAUTHORIZED", "Your session has expired. Please sign in again and retry.",
                    StatusCodes.Status401Unauthorized);

            args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;
            await EnrichAgentAsync(args);
            List<BulkMembershipRowDto> rows;
            try
            {
                await using var stream = file.OpenReadStream();
                rows = _parser.Parse(stream, file.FileName, args.businessCode, onboardedById, args.organizationCode, args.Id);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogWarning(ex, "Unsupported membership file type uploaded: {FileName}", file.FileName);
                return Error("FILE_TYPE_INVALID", "We couldn't read that file format. Please upload a CSV or Excel file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse membership upload file: {FileName}", file.FileName);
                return Error("FILE_PARSE_ERROR",
                    "We couldn't read this file. Please check that it's a valid CSV/Excel export and try again.");
            }
            if (rows.Count == 0)
                return Error("FILE_EMPTY", "This file doesn't contain any membership records to upload.");

            var validationErrors = await _validator.ValidateAllAsync(rows, args);
            if (validationErrors.Any())
            {
                return UnprocessableEntity(new ApiErrorResponse
                {
                    Code = "VALIDATION_FAILED",
                    Message = "We found problems with some rows. No records were saved — please fix and re-upload.",
                    Details = validationErrors
    .GroupBy(e => e.RowNumber)
    .ToDictionary(
        g => g.Key.ToString(),
        g => string.Join("; ", g.SelectMany(e => e.Messages)))
                });
            }

            try
            {
                var result = await _memberShipUploadService.ProcessBulkMembershipUploadAsync(rows, args);

                return result.FailureCount == 0
                    ? Ok(result)
                    : StatusCode(StatusCodes.Status207MultiStatus, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Membership upload processing failed for business {BusinessCode}", args.businessCode);
                return Error("UPLOAD_PROCESSING_FAILED",
                    "Something went wrong while processing your upload. Please try again, and contact support if the problem continues.",
                    StatusCodes.Status500InternalServerError);
            }
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

        [HttpGet("memberships")]
        public async Task<IActionResult> GetCooperativesByAgent([FromQuery] ListQueryParams query)
        {
            ResolveAgentQueryContext(query);

            return await ExecuteAsync(async () =>
            {
                if (!await _onBoardingManager.IsUserInRoleAsync(query.UserId, "Agent"))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "The supplied user is not a Agent.");

                return await _agentManager.GetMembershipsByAgent(query.UserId, query);
            },
            result => Ok(BuildPagedResponse(result, query.PageNumber, query.PageSize)));
        }
        [HttpPost("create-membership")]
        public async Task<IActionResult> CreateMembershipUser([FromBody] MemberShipRequestModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();


            return await ExecuteAsync(async () =>
            {
                args.OnboardedByUserId = CurrentUserId;
                NormalizeOrganizationCode(args);
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

                if (!await _onBoardingManager.IsUserInRoleAsync(args.UserId, "Agent"))
                    throw new BaseException(ExceptionType.BAD_REQUEST,"The supplied user is not a Agent.");

                await EnrichAgentAsync(args);

                return await _onBoardingManager.CreateMembershipUser(model, args);
            },
            request => Ok(Success(request, request.Message)));

        }
        [HttpGet("farmers")]
        public async Task<IActionResult> GetFarmersByAgent([FromQuery] ListQueryParams query)
        {
            ResolveAgentQueryContext(query);

            return await ExecuteAsync(async () =>
            {
                if (!await _onBoardingManager.IsUserInRoleAsync(query.UserId, "Agent"))
                    throw new BaseException(ExceptionType.BAD_REQUEST,"The supplied user is not a Agent.");

                return await _agentManager.GetFarmersByAgent(query.UserId, query);
            },
            result => Ok(BuildPagedResponse(result, query.PageNumber, query.PageSize)));
        }
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetAgentDashboardData([FromQuery] ListQueryParams query)
        {
            ResolveAgentQueryContext(query);

            return await ExecuteAsync(async () =>
            {
                if (!await _onBoardingManager.IsUserInRoleAsync(query.UserId, "Agent"))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "The supplied user is not a Agent.");

                return await _agentManager.GetAgentDashboardData(query);
            },
            result => Ok(new Response<AgentDashboardDto>
            {
                Data = result,
                Message = result != null ? ResponseMessages.Successful : "Could not retrieve data",
                Succeeded = result != null,
                Code = result != null ? ResponseCodes.Success : ResponseCodes.Failed
            }));
        }
        [HttpPost("update-profile")]
        [AllowAnonymous]
        public async Task<IActionResult> EditAgentUser([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            args.OnboardedByUserId = CurrentUserId;
            args.organizationCode = args.organizationCode ?? OrganizationCode;

            return await ExecuteAsync(
                () => _onBoardingManager.EditAgentUser(model, args),
                request => Ok(Success(request, ResponseMessages.Successful)));
        }
        private void ResolveAgentQueryContext(ListQueryParams query)
        {
            if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                query.organizationCode = OrganizationCode;

            query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
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
