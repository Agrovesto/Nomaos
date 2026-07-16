using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.V2.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Agrovesto.Backend.API.Controllers
{
    //[ApiController]
    //[Authorize]
    // [ValidateModelFilter]
    public class BaseController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BaseController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public IActionResult ValidationProblemResponse()
        {
            return BadRequest(new Response<Dictionary<string, string>>()
            {
                Message = "Validation failed.",
                Succeeded = false,
                Data = ModelState
                    .Where(ms => ms.Value?.Errors.Any() == true)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                    )
            });


        }
        public ObjectResult Error(string code, string message, int statusCode = StatusCodes.Status400BadRequest)
        {
            return StatusCode(statusCode, new ApiErrorResponse
            {
                Code = code,
                Message = message
            });
        }
        public string? DomainHeader
        {
            get
            {
                var headers = _httpContextAccessor.HttpContext?.Request.Headers;
                var header = _httpContextAccessor.HttpContext?.Request.Headers["Domain"];
                if (string.IsNullOrEmpty(header)) return string.Empty;
                return header;
            }
        }

        protected JsonWebToken? TryReadJwt()
        {
            var token = Token;

            if (string.IsNullOrWhiteSpace(token))
                return null;

            var handler = new JsonWebTokenHandler();

            if (!handler.CanReadToken(token))
                return null;

            try
            {
                return handler.ReadJsonWebToken(token);
            }
            catch
            {
                return null;
            }
        }
        public string? CurrentUserId =>
    TryReadJwt()?
        .Claims.FirstOrDefault(x => x.Type.Equals("sub", StringComparison.OrdinalIgnoreCase))
        ?.Value;
        public string? CurrentUserEmail =>
            TryReadJwt()?
                .Claims.FirstOrDefault(x => x.Type.Equals("email", StringComparison.OrdinalIgnoreCase))
                ?.Value;
     
        
        
        public string? CurrentUserRole
        {
            get
            {
                var jwt = TryReadJwt();
                if (jwt == null) return null;

                var roleClaim = jwt.Claims.FirstOrDefault(c =>
                    c.Type.Equals("http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                                  StringComparison.OrdinalIgnoreCase));

                if (roleClaim != null) return roleClaim.Value;

                roleClaim = jwt.Claims.FirstOrDefault(c => c.Type.Equals("Roles", StringComparison.OrdinalIgnoreCase));
                return roleClaim?.Value;
            }
        }
        public IEnumerable<string> CurrentUserRoles
        {
            get
            {
                var jwt = TryReadJwt();
                if (jwt == null) return Enumerable.Empty<string>();

                var roles = jwt.Claims
                    .Where(c =>
                        c.Type.Equals("http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                                      StringComparison.OrdinalIgnoreCase) ||
                        c.Type.Equals("Roles", StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Value)
                    .Distinct()
                    .ToList();

                return roles;
            }
        }
        public string? OrganizationCode
        {
            get
            {
                var jwt = TryReadJwt();
                if (jwt == null)
                    return null;

                var claimValue = jwt.Claims
                    .FirstOrDefault(x => x.Type.Equals("Organizations", StringComparison.OrdinalIgnoreCase))
                    ?.Value;

                if (string.IsNullOrWhiteSpace(claimValue))
                    return null;

                try
                {
                    var organizations = JsonSerializer.Deserialize<List<OrganizationClaim>>(claimValue);

                    return organizations?.Any() == true
                        ? string.Join(",", organizations.Select(o => o.OrganizationCode))
                        : null;
                }
                catch
                {
                    return null;
                }
            }
        }


        public string Token
        {
            get
            {
                if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(HeaderNames.Authorization, out var tokenValues) == true)
                {
                    var token = tokenValues.ToString().Trim(); 

                    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return token.Substring("Bearer ".Length).Trim(); 
                    }
                }
                return string.Empty;
            }
        }

      
        public class OrganizationClaim
        {
            public string? OrganizationCode { get; set; }
            public string? OrganizationName { get; set; }
        }

    }

}