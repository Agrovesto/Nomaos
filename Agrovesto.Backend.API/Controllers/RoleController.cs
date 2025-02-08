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

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/role")]
   // [ApiController]
    public class RoleController : ControllerBase
    { 

        private readonly IRoleManager _roleManager;
        public RoleController(IRoleManager roleManager)
        {
            _roleManager = roleManager;
        }


        [HttpPost("create")]
        public async Task<IActionResult> CreateRole([FromBody] RoleModel roleModel)
        {
            // var isAdmin = User.Claims.ToArray();
            // if (!isAdmin) throw new BadRequestException("Not Authorized");
            var data = await _roleManager.CreateRoleAsync(roleModel);
            return Ok(new AgrovestoResponse<RoleModel>()
            {
                Message = "",
                Result = data.Result,
               
            }); 
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            // var isAdmin = User.Claims.ToArray();
            // if (!isAdmin) throw new BadRequestException("Not Authorized");
            var data = await _roleManager.GetRolesAsync();
            return Ok(new PagedListResponse<List<RoleModel>>()
            {
                Message = "",
              Data= data.Data,
              Count= data.Count

            });
        }


        [HttpGet("role/{roleId}")]
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
    }
}
