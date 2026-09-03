
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BulkUploadController : BaseController
    {
        
        private readonly IBulkFarmerUploadService _bulkFarmerUploadService;
        private readonly ILogger<BulkUploadController> _logger;

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
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;

            return await HandleBulkFarmerUploadAsync(file, args, _bulkFarmerUploadService, _logger);
        }
    }
}
