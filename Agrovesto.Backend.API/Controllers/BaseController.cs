using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                var handler = new JwtSecurityTokenHandler();
                var jst = handler.ReadJwtToken(Token);

                var claims = jst.Claims.FirstOrDefault(x => x.Type.Equals("role", StringComparison.OrdinalIgnoreCase));
                return claims?.Value;
            }
        }

        public string CurrentUserEmail
        {
            get
            {
                var token = Token;
                if (string.IsNullOrEmpty(token)) return null; // Ensure a token exists

                var handler = new JwtSecurityTokenHandler();

                // Ensure the token is a well-formed JWT before reading it
                if (!handler.CanReadToken(token))
                {
                    throw new SecurityTokenException("Invalid JWT format");
                }

                var jst = handler.ReadJwtToken(token);
                var claims = jst.Claims.FirstOrDefault(x => x.Type.Equals("email", StringComparison.OrdinalIgnoreCase));

                return claims?.Value;
            }
        }


        public string CurrentUserId
        {
            get
            {
                var token = Token;
                if (string.IsNullOrEmpty(token)) return null; // Ensure a token exists

                var handler = new JwtSecurityTokenHandler();

                // Ensure the token is a well-formed JWT before reading it
                if (!handler.CanReadToken(token))
                {
                    throw new SecurityTokenException("Invalid JWT format");
                }

                var jst = handler.ReadJwtToken(token);
                var claims = jst.Claims.FirstOrDefault(x => x.Type.Equals("sub", StringComparison.OrdinalIgnoreCase));

                return claims?.Value;
            }
        }

        //public string CurrentUserId
        //{
        //    get
        //    {
        //        if (string.IsNullOrEmpty(Token)) return null;
        //        var handler = new JwtSecurityTokenHandler();
        //        var jst = handler.ReadJwtToken(Token);

        //        var claims = jst.Claims.FirstOrDefault(x => x.Type.Equals("sub", StringComparison.OrdinalIgnoreCase));
        //        return claims?.Value;
        //    }
        //}
    }

}