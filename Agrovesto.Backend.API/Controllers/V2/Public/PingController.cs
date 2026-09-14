using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Public
{
    [ApiController]
    public class PingController : ControllerBase
    {
        [HttpGet("/api/v2/account/ping")]
        public IActionResult AccountPing()
        {
            return Ok(new { status = "OK", endpoint = "api/v2/account/ping" });
        }

        [HttpGet("/api/v2/user/ping")]
        public IActionResult UserPing()
        {
            return Ok(new { status = "OK", endpoint = "api/v2/user/ping" });
        }

        [HttpGet("/api/v2/location/ping")]
        public IActionResult LocationPing()
        {
            return Ok(new { status = "OK", endpoint = "api/v2/location/ping" });
        }

        [HttpGet("/api/v2/business/ping")]
        public IActionResult BusinessPing()
        {
            return Ok(new { status = "OK", endpoint = "api/v2/business/ping" });
        }

        [HttpGet("/api/v2/farm/ping")]
        public IActionResult FarmPing()
        {
            return Ok(new { status = "OK", endpoint = "api/v2/farm/ping" });
        }

        [HttpGet("/api/v2/authentication/ping")]
        public IActionResult AuthenticationPing()
        {
            return Ok(new { status = "OK", endpoint = "api/v2/authentication/ping" });
        }
    }
}
