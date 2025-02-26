using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                var header = _httpContextAccessor.HttpContext?.Request.Headers["Domain"];
                if (string.IsNullOrEmpty(header)) return string.Empty;
                return header;
            }
        }
        public string Token
        {
            get
            {
                var token = _httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization];
                if (!AuthenticationHeaderValue.TryParse(token, out var headerValue))
                    return string.Empty;
                return headerValue.Parameter;
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

        public string CurrentUserId
        {
            get
            {
                if (string.IsNullOrEmpty(Token)) return null;
                var handler = new JwtSecurityTokenHandler();
                var jst = handler.ReadJwtToken(Token);

                var claims = jst.Claims.FirstOrDefault(x => x.Type.Equals("sub", StringComparison.OrdinalIgnoreCase));
                return claims?.Value;
            }
        }
    }

    //public class BaseController : ControllerBase
    //{

    //    public string Token
    //    {
    //        get
    //        {
    //            var token = HttpContext.Request.Headers[HeaderNames.Authorization];
    //            if (!AuthenticationHeaderValue.TryParse(token, out var headerValue))
    //                return string.Empty;
    //            var parameter = headerValue.Parameter;
    //            return parameter;
    //        }
    //    }


    //    public string CurrentUserRole
    //    {
    //        get
    //        {
    //            if (string.IsNullOrEmpty(Token)) return null;
    //            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
    //            JwtSecurityToken jst = handler.ReadJwtToken(Token);

    //            var claims = jst.Claims.FirstOrDefault(x => x.Type.ToLower() == "Role");
    //            if (claims == null)
    //            {
    //                return null;
    //            }
    //            return claims.Value;
    //        }
    //    }


    //    public string CurrentUserId
    //    {
    //        get
    //        {
    //            if (string.IsNullOrEmpty(Token)) return null;
    //            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
    //            JwtSecurityToken jst = handler.ReadJwtToken(Token);

    //            var claims = jst.Claims.FirstOrDefault(x => x.Type.ToLower() == "sub");
    //            if (claims == null)
    //            {
    //                return null;
    //            }
    //            return claims.Value;

    //        }
    //    }
    //}
}