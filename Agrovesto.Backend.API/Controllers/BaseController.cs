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
        public string DomainHeader
        {
            get
            {
                var headers = _httpContextAccessor.HttpContext?.Request.Headers;
                var header = _httpContextAccessor.HttpContext?.Request.Headers["Domain"];
                if (string.IsNullOrEmpty(header)) return string.Empty;
                return header;
            }
        }
        //public string Token
        //{
        //    get
        //    {
        //        var token = _httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization];
        //        if (!AuthenticationHeaderValue.TryParse(token, out var headerValue))
        //            return string.Empty;
        //        return headerValue.Parameter;
        //    }
        //}

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
        //    public string? CurrentUserRole =>
        //TryReadJwt()?
        //    .Claims.FirstOrDefault(x => x.Type.Equals("role", StringComparison.OrdinalIgnoreCase))
        //    ?.Value;
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

                // fallback to "Roles" claim
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
                    var token = tokenValues.ToString().Trim(); // Trim to remove any unwanted spaces

                    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return token.Substring("Bearer ".Length).Trim(); // Extract the token part
                    }
                }
                return string.Empty;
            }
        }

      
        public class OrganizationClaim
        {
            public string OrganizationCode { get; set; }
            public string OrganizationName { get; set; }
        }

    }

}