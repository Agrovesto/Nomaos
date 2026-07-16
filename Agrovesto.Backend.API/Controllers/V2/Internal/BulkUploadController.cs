using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/[controller]")]
    [ApiController]
    public class BulkUploadController : BaseController
    {
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly IBusinessManager _businessManager;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private readonly IBulkFarmerUploadService _bulkFarmerUploadService;
        private readonly ILogger<AuthenticationController> _logger;
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
        ".xlsx", ".xls",    // Excel formats
        ".csv"                               // OpenDocument (LibreOffice)
            };
        public BulkUploadController(IHttpContextAccessor httpContextAccessor, IOnBoardingManager onBoardingManager,
            IBusinessManager businessManager, IBulkFarmerUploadService bulkFarmerUploadService, ILogger<AuthenticationController> logger) : base(httpContextAccessor)
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
        private IActionResult Error(string code, string message, int statusCode = StatusCodes.Status400BadRequest)
        {
            return StatusCode(statusCode, new ApiErrorResponse
            {
                Code = code,
                Message = message
            });
        }

    }
}
