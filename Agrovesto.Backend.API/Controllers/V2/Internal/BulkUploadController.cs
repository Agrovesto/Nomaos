
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/[controller]")]
    [ApiController]
    public class BulkUploadController : BaseController
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private readonly IBulkFarmerUploadService _bulkFarmerUploadService;
        private readonly ILogger<BulkUploadController> _logger;
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
        ".xlsx", ".xls",  
        ".csv"  
            };
        public BulkUploadController(IHttpContextAccessor httpContextAccessor,  
            IBulkFarmerUploadService bulkFarmerUploadService,
            ILogger<BulkUploadController> logger) : base(httpContextAccessor)
        {
            _bulkFarmerUploadService = bulkFarmerUploadService;
            _logger = logger;
        }
        [HttpPost("bulk-onboard")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> BulkOnboard(IFormFile file, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            args.OnboardedByUserId = CurrentUserId;
            NormalizeOrganizationCode(args); 

            return await HandleBulkFarmerUploadAsync(file, args, _bulkFarmerUploadService, _logger);
        }
    }
}
