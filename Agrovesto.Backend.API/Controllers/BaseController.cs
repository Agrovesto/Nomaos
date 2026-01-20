using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

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


        public string CurrentUserRole
        {
            get
            {
                if (string.IsNullOrEmpty(Token)) return null;
                var handler = new JsonWebTokenHandler();
                var jst = handler.ReadJsonWebToken(Token);

                var claims = jst.Claims.FirstOrDefault(x => x.Type.Equals("role", StringComparison.OrdinalIgnoreCase));
                return !string.IsNullOrEmpty(claims?.Value) ? claims?.Value : null;
            }
        }

        public string CurrentUserEmail
        {
            get
            {
                var token = Token;
                if (string.IsNullOrEmpty(token)) return null; // Ensure a token exists

                var handler = new JsonWebTokenHandler();

                // Ensure the token is a well-formed JWT before reading it
                if (!handler.CanReadToken(token))
                {
                    throw new SecurityTokenException("Invalid JWT format");
                }

                var jst = handler.ReadJsonWebToken(token);
                var claims = jst.Claims.FirstOrDefault(x => x.Type.Equals("email", StringComparison.OrdinalIgnoreCase));

                return !string.IsNullOrEmpty(claims?.Value) ? claims?.Value : null;
            }
        }

        public string? CurrentUserId
        {
            get
            {
                try
                {
                    var token = Token;

                    if (string.IsNullOrWhiteSpace(token))
                        return null;

          
                    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        token = token.Substring("Bearer ".Length).Trim();

                    var handler = new JsonWebTokenHandler();

                    if (!handler.CanReadToken(token))
                        return null; 

                    var jwt = handler.ReadJsonWebToken(token);

                    return jwt.Claims
                        .FirstOrDefault(x => x.Type.Equals("sub", StringComparison.OrdinalIgnoreCase))
                        ?.Value;
                }
                catch
                {
                    return null;
                }
            }
        }

        //public string CurrentUserId
        //{
        //    get
        //    {
        //        var token = Token;
        //        if (string.IsNullOrEmpty(token)) return null; // Ensure a token exists

        //        var handler = new JsonWebTokenHandler();

        //        // Ensure the token is a well-formed JWT before reading it
        //        if (!handler.CanReadToken(token))
        //        {
        //            throw new SecurityTokenException("Invalid JWT format");
        //        }

        //        var jst = handler.ReadJsonWebToken(token);
        //        var claims = jst.Claims.FirstOrDefault(x => x.Type.Equals("sub", StringComparison.OrdinalIgnoreCase));

        //        return !string.IsNullOrEmpty(claims?.Value) ? claims?.Value : null;
        //    }
        //}
        //public string OrganizationCode
        //{
        //    get
        //    {
        //        var token = Token;
        //        if (string.IsNullOrEmpty(token)) return null; // Ensure a token exists

        //        var handler = new JsonWebTokenHandler();

        //        // Ensure the token is a well-formed JWT before reading it
        //        if (!handler.CanReadToken(token))
        //        {
        //            throw new SecurityTokenException("Invalid JWT format");
        //        }

        //        var jst = handler.ReadJsonWebToken(token);
        //        var claims = jst.Claims.FirstOrDefault(x => x.Type.Equals("Organizations", StringComparison.OrdinalIgnoreCase));


        //        //[{"OrganizationCode":"KIC115","OrganizationName":"Kickstart Abj"}]
        //        return !string.IsNullOrEmpty(claims?.Value) ? claims?.Value : null;
        //    }
        //}

        public string OrganizationCode
        {
            get
            {
                var token = Token;
                if (string.IsNullOrEmpty(token))
                    return null;

                var handler = new JsonWebTokenHandler();

                if (!handler.CanReadToken(token))
                    throw new SecurityTokenException("Invalid JWT format");

                var jst = handler.ReadJsonWebToken(token);
                var claims = jst.Claims.FirstOrDefault(x =>
                    x.Type.Equals("Organizations", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(claims?.Value))
                    return null;

                try
                {
                    // Deserialize the JSON array
                    var organizations = System.Text.Json.JsonSerializer.Deserialize<List<OrganizationClaim>>(claims.Value);

                    // Return a comma-separated list if there are multiple organization codes
                    return organizations != null && organizations.Count > 0
                        ? string.Join(",", organizations.Select(o => o.OrganizationCode))
                        : null;
                }
                catch
                {
                    // Handle invalid JSON or unexpected format
                    return null;
                }
            }
        }

        // Helper class to match the claim structure
        public class OrganizationClaim
        {
            public string OrganizationCode { get; set; }
            public string OrganizationName { get; set; }
        }

    }

}