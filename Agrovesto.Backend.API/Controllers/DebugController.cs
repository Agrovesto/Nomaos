using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/debug")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        [HttpGet("claims")]
        [Authorize]
        public IActionResult GetClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var isAgent = User.IsInRole("Agent");
            var isOrganization = User.IsInRole("Organization");

            return Ok(new
            {
                Name = User.Identity?.Name,
                IsAuthenticated = User.Identity?.IsAuthenticated,
                IsAgent = isAgent,
                IsOrganization = isOrganization,
                Claims = claims
            });
        }
    }
}
