using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [ApiController]
    [Route("api/admin/access")]
    [Authorize]
    public class AdminAccessController : BaseController
    {
        private readonly IRoleAccessManager _access;

        public AdminAccessController(IHttpContextAccessor httpContextAccessor, IRoleAccessManager access) : base(httpContextAccessor)
        {
            _access = access;
        }

        [AllowAnonymous]
        // ---- Roles ----
        [HttpGet("roles")]
        //[HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> ListRoles(CancellationToken ct) => Respond(await _access.ListRolesAsync(ct));

        [HttpGet("roles/{id}")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> GetRole(string id, CancellationToken ct) => Respond(await _access.GetRoleAsync(id, ct));

        [HttpPost("roles")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest req, CancellationToken ct)
            => Respond(await _access.CreateRoleAsync(req, ct));

        [HttpPut("roles/{id}")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] SaveRoleRequest req, CancellationToken ct)
            => Respond(await _access.UpdateRoleAsync(id, req, ct));

        [HttpDelete("roles/{id}")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> DeleteRole(string id, CancellationToken ct)
            => Respond(await _access.DeleteRoleAsync(id, ct));

        [HttpPut("roles/{id}/permissions")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> SetRolePermissions(string id, [FromBody] SetRolePermissionsRequest req, CancellationToken ct)
            => Respond(await _access.SetRolePermissionsAsync(id, req, ct));

        // ---- Permissions ----
        [HttpGet("permissions")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> ListPermissions(CancellationToken ct) => Respond(await _access.ListPermissionsAsync(ct));

        [HttpPost("permissions")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest req, CancellationToken ct)
            => Respond(await _access.CreatePermissionAsync(req, ct));

        [HttpPut("permissions/{id:int}")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> UpdatePermission(int id, [FromBody] UpdatePermissionRequest req, CancellationToken ct)
            => Respond(await _access.UpdatePermissionAsync(id, req, ct));

        [HttpDelete("permissions/{id:int}")]
        [HasPermission(Permissions.Roles.Manage)]
        public async Task<IActionResult> DeletePermission(int id, CancellationToken ct)
            => Respond(await _access.DeletePermissionAsync(id, ct));

        private IActionResult Respond<T>(AccessResult<T> r) =>
            r.Succeeded
                ? StatusCode(r.StatusCode, new Response<T>()
                {
                    Code = AdminResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = r.Data
                })
                : StatusCode(r.StatusCode, new Response<object>()
                {
                    Code = AdminResponseCodes.Failed,      // use whatever your failure constant is
                    Succeeded = false,
                    Message = r.Error
                });
    }
}
