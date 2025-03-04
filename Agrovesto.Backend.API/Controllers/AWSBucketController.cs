using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Infrastructure.ExternalServices.ImageRecognition;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AWSBucketController : ControllerBase
    {
        private readonly IFaceManager _faceManager;
        public AWSBucketController(IFaceManager faceManager)
        {
           _faceManager = faceManager;
        }
  
        [HttpPost("compare-face")]
        public async Task<IActionResult> CompareFaces([FromForm] IFormFile uploadedImage,[FromQuery] string userId, [FromQuery] int targetImageId)
        {
            var result = await _faceManager.CompareFaces(uploadedImage,userId, targetImageId);
            if (result == null)
            {
                return StatusCode(500, "An error occurred while processing the images.");
            }
            return Ok(result);
        }


    }
}
