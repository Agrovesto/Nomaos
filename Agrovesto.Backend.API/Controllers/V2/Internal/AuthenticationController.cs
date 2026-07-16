using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
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
    [Route("api/v2/account")]
    [ApiController]
    public class AuthenticationController : BaseController
    {
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly IBusinessManager _businessManager;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        private readonly IBulkFarmerUploadService _bulkFarmerUploadService;
        private readonly ILogger<AuthenticationController> _logger;
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
        ".xlsx", ".xls",  
        ".csv"         
            };
        public AuthenticationController(IHttpContextAccessor httpContextAccessor,
            IOnBoardingManager onBoardingManager,
            IBusinessManager businessManager, IBulkFarmerUploadService bulkFarmerUploadService, 
            ILogger<AuthenticationController> logger) : base(httpContextAccessor)
        {
            _onBoardingManager = onBoardingManager;
            _businessManager = businessManager;
            _bulkFarmerUploadService = bulkFarmerUploadService;
            _logger = logger;
        }
        [HttpPost("bulk-onboard")]

        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> BulkOnboard(
        IFormFile file,
        [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            if (file is null || file.Length == 0)
                return Error("FILE_MISSING", "Please choose a file to upload before submitting.");

            if (file.Length > MaxFileSizeBytes)
                return Error("FILE_TOO_LARGE",
                    $"That file is too large. Please upload a file smaller than {MaxFileSizeBytes / (1024 * 1024)} MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return Error("FILE_TYPE_INVALID",
                    "We couldn't read that file. Please upload a CSV or Excel file (.csv, .xlsx, .xls).");

            args.OnboardedByUserId = CurrentUserId;
            NormalizeOrganizationCode(args);
         
            if (CurrentUserId != null && CurrentUserRole != args.userType)
            {
                await EnrichArgsByRoleAsync(args);
            }

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
     
        [HttpPost("create-account")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] OnboardingRequestModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {

                args.OnboardedByUserId = CurrentUserId;
                NormalizeOrganizationCode(args);
                if (CurrentUserId != null && CurrentUserRole != args.userType)
                {
                    await EnrichArgsByRoleAsync(args);
                }

                var request = await _onBoardingManager.CreateBusinessUser(model, args);


                return Ok(new Response<OnboardingResponseModel>()
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
                    Message = ex.Message,  Succeeded = false,
                });
            }

        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] ConfirmOtpRequest model)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {

                var request = await _onBoardingManager.ConfirmOtp(model);


                return Ok(new Response<bool>()
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
        [HttpPost("resend-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {

                await _onBoardingManager.ResendOnboardingOtp(model, args);


                return Ok(new Response<string>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = "OTP sent"
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

        [HttpPost("create-profile")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateProfile(
        [FromBody] ProfileRequestModel model,
        [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
                args.OnboardedByUserId = CurrentUserId;
                NormalizeOrganizationCode(args);
                if (string.IsNullOrWhiteSpace(args.UserId))
                {
                    throw new BaseException(
                        ExceptionType.BAD_REQUEST,
                        "UserId must be provided to create a profile for the farmer."
                    );
                }
                if (CurrentUserId != null)
                {
                    await EnrichArgsByRoleAsync(args);
                }

                var result = await _onBoardingManager.CreateUserProfile(model, args);

                return Ok(new Response<(bool status, string message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful ,          
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

        [HttpPost("create-organization-profile")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrganization(
        [FromBody] OrganizationOnboardingModel model,
        [FromQuery] OrganizationOnboardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
                args.onboardedByUserId = CurrentUserId;

                var result = await _onBoardingManager.CreateOrganization(model, args);

                return Ok(new Response<OrganizationResponseModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
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
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model, [FromQuery] string? businessCode)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
                var createUser = await _onBoardingManager.Login(model, businessCode);
                if (createUser.Status)
                {
                    return Ok(new Response<TokenModel>()
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = createUser.Status,
                        Message = createUser.Message,
                        Data = createUser.Response
                    });
                }
                else
                {
                    return BadRequest(new Response<TokenModel>()
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = createUser.Status,
                        Message = createUser.Message,
                        Data = createUser.Response
                    });
                }


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
        [HttpPost("assign-agents-to-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignFarmerToAgents([FromQuery] AssignmentArgs args)
        {

            try
            {
                args.UserId = CurrentUserId;

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

        [HttpPost("assign-cooperatives-to-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignFarmerToCooperatives([FromQuery] AssignmentArgs args)
        {

            try
            {
                args.UserId ??= CurrentUserId;
                args.OnboardedByUserId = CurrentUserId;
                NormalizeOrganizationCode(new OnBoardingArgs
                {
                    organizationCode = args.organizationCode,
                });
                var request = await _onBoardingManager.AssignFarmerToCooperatives(args);


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
            //   args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
            var agent = await _businessManager.GetAgentDetailByUserId(CurrentUserId);

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
      
    }
}
