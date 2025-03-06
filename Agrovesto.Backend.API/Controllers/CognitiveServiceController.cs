using Agrovesto.Backend.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CognitiveServiceController : ControllerBase
    {
        private readonly IFaceManager _faceManager;
        public CognitiveServiceController(IFaceManager faceManager)
        {
            _faceManager = faceManager;
        }

        [HttpPost("compare-face")]
        public async Task<IActionResult> CompareFaces(IFormFile uploadedImage, [FromQuery] string userId, [FromQuery] int targetImageId)
        {
            var result = await _faceManager.CompareFaces(uploadedImage, userId, targetImageId);
            if (result == null)
            {
                return StatusCode(500, "An error occurred while processing the images.");
            }
            return Ok(result);
        }


    }
}

