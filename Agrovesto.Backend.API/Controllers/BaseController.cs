using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

namespace Agrovesto.Backend.API.Controllers
{
    [ApiController]
    [Authorize]
    // [ValidateModelFilter]
    public class BaseController : ControllerBase
    {
        public string Token
        {
            get
            {
                var token = HttpContext.Request.Headers[HeaderNames.Authorization];
                if (!AuthenticationHeaderValue.TryParse(token, out var headerValue))
                    return string.Empty;
                var parameter = headerValue.Parameter;
                return parameter;
            }
        }
        //public string FilterToken(HttpRequest httpRequest)
        //{
        //    string token = httpRequest.Headers["Authorization"];
        //    if (string.IsNullOrEmpty(token))
        //    {
        //        return null;
        //    }
        //    token = token.Replace("Bearer", "");
        //    token = token.Replace("bearer", "");
        //    return token.Trim();
        //}
        //public string Token()
        //{
        //    return Request.FilterToken();
        //}

        public string CurrentUserRole
        {
            get
            {
                if (string.IsNullOrEmpty(Token)) return null;
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jst = handler.ReadJwtToken(Token);

                var claims = jst.Claims.FirstOrDefault(x => x.Type.ToLower() == "Role");
                if (claims == null)
                {
                    return null;
                }
                return claims.Value;
            }
        }


        public string CurrentUserId
        {
            get
            {
                if (string.IsNullOrEmpty(Token)) return null;
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jst = handler.ReadJwtToken(Token);

                var claims = jst.Claims.FirstOrDefault(x => x.Type.ToLower() == "sub");
                if (claims == null)
                {
                    return null;
                }
                return claims.Value;

            }
        }
    }
}