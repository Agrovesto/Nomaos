using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.Models.Common.Constants;
using System.ComponentModel.DataAnnotations;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api")]
    // [ApiController]
    public class RoleController : ControllerBase
    {

        private readonly IRoleManager _roleManager;
        public RoleController(IRoleManager roleManager)
        {
            _roleManager = roleManager;
        }
        [HttpPost("role/update-role")]
        public async Task<IActionResult> UpdateRole([FromBody] RoleModel roleModel)
        {
            // var isAdmin = User.Claims.ToArray();
            // if (!isAdmin) throw new BadRequestException("Not Authorized");
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {

                var data = await _roleManager.UpdateRoleAsync(roleModel.RoleId,roleModel);
                return Ok(new AgrovestoResponse<RoleModel>()
                {
                    Message = "",
                    Result = data.Result,

                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }


        [HttpPost("role/create")]
        public async Task<IActionResult> CreateRole([FromBody] RoleModel roleModel)
        {
            // var isAdmin = User.Claims.ToArray();
            // if (!isAdmin) throw new BadRequestException("Not Authorized");
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {

                var data = await _roleManager.CreateRoleAsync(roleModel);
                return Ok(new AgrovestoResponse<RoleModel>()
                {
                    Message = "",
                    Result = data.Result,

                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }

        [HttpGet("role/roles")]
        public async Task<IActionResult> GetRoles()
        {
            // var isAdmin = User.Claims.ToArray();
            // if (!isAdmin) throw new BadRequestException("Not Authorized");
            var data = await _roleManager.GetRolesAsync();
            return Ok(new PagedListResponse<List<RoleModel>>()
            {
                Message = "",
                Data = data.Data,
                Count = data.Count

            });
        }
        [HttpGet("v2/roles/admin")]
        public async Task<IActionResult> GetAdminRolesAsync()
        {
            // var isAdmin = User.Claims.ToArray();
            // if (!isAdmin) throw new BadRequestException("Not Authorized");
            var data = await _roleManager.GetAdminRolesAsync();
            return Ok(new PagedListResponse<List<RoleModel>>()
            {
                Message = "",
                Data = data.Data,
                Count = data.Count,
                Succeeded=true,
                Code= ResponseCodes.Success

            });
        }

         [HttpGet("role/roles/business")]
        public async Task<IActionResult> GetBusinessRoles()
        {
            // var isAdmin = User.Claims.ToArray();
            // if (!isAdmin) throw new BadRequestException("Not Authorized");
            var data = await _roleManager.GetBusinessRoles();
            return Ok(new PagedListResponse<List<RoleModel>>()
            {
                Message = "",
                Data = data.Data,
                Count = data.Count

            });
        }


        [HttpGet("role/role/{roleId}")]
        public async Task<IActionResult> GetRole(string roleId)
        {
            // var isAdmin = User.Claims.ToArray();
            // if (!isAdmin) throw new BadRequestException("Not Authorized");
            var data = await _roleManager.GetRoleByIdAsync(roleId);
            return Ok(new Response<RoleModel>()
            {
                Message = "",
                Data = data.Data

            });
        }
        [HttpPatch("role/assignUserToRole/{userId}/{roleId}")]
        public async Task<IActionResult> AssignUserToRole(string userId, string roleId)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                var data = await _roleManager.AddUserToRole(userId, roleId);

                return Ok(new AgrovestoResponse<string>()
                {
                    Message = "",
                    Result = data.ToString(),

                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }
            [HttpPatch("role/removeuser-from-role/{userId}/{roleId}")]
        public async Task<IActionResult> RemoveUserFromRole(string userId, string roleId)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                var data = await _roleManager.RemoveUserFromRole(userId, roleId);

                return Ok(new AgrovestoResponse<string>()
                {
                    Message = "",
                    Result = data.ToString(),

                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }
                [HttpPatch("role/change-from-role/{userId}/{prevroleId}/{newroleId}")]
        public async Task<IActionResult> ReAssignUserRole(string userId, string prevroleId, string newroleId)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                var data = await _roleManager.ReAssignUserRole(userId, prevroleId, newroleId);

                return Ok(new AgrovestoResponse<string>()
                {
                    Message = "",
                    Result = data.ToString(),

                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }
    }
}
