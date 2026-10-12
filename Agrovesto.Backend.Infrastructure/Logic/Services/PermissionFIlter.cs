using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OfficeOpenXml.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public class HasPermissionAttribute : TypeFilterAttribute
    {
        public HasPermissionAttribute(string permission) : base(typeof(HasPermissionFilter))
            => Arguments = new object[] { permission };
    }

    public class HasPermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        private readonly IPermissionService _permissions;

        public HasPermissionFilter(string permission, IPermissionService permissions)
        { _permission = permission; _permissions = permissions; }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) { context.Result = new UnauthorizedResult(); return; }   // 401

            if (!await _permissions.HasPermissionAsync(userId, _permission, context.HttpContext.RequestAborted))
                context.Result = new ObjectResult(new { message = "You do not have permission to perform this action." })
                { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
