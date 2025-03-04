using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AWSBucketController : ControllerBase
    {
        private readonly IAmazonS3 _amazonS3;
        public AWSBucketController(IAmazonS3 amazonS3)
        {
            _amazonS3 = amazonS3;
        }
        [HttpGet("list")]
        public async Task<IActionResult> ListAsync()
        {
            var data = await _amazonS3.ListBucketsAsync();
            var buckets = data.Buckets.Select(b => { return b.BucketName; });
            return Ok(buckets);
        }
    }
}
