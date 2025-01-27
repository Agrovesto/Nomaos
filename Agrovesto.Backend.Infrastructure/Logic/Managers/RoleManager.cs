using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class RoleManager : IRoleManager
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        public RoleManager(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }


        public async Task<AgrovestoResponse<RoleModel>> CreateRoleAsync(RoleModel model)
        {
            if (await _roleManager.RoleExistsAsync(model.RoleName))
            {
                return new AgrovestoResponse<RoleModel>()
                {
                    Message = $"Role '{model.RoleName}' already exists.",
                    Result = null
                };
            }

            var role = model.Map();
            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                return new AgrovestoResponse<RoleModel>()
                {
                    Message = $"Role '{model.RoleName}' added.",
                    Result = role.Map()
                };
            }
            else
            {
                return new AgrovestoResponse<RoleModel>()
                {
                    Message = $"Role '{result.Errors}' added.",
                    Result = null
                };
            }
        }


        public async Task<PagedListResponse<List<RoleModel>>> GetRolesAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();

            var roleMap= roles.Select(x => x.Map()).ToList();
            return new PagedListResponse<List<RoleModel>>
            {
                Data = roleMap,
                Code = "00",
                Message="",
                Count=roleMap.Count,
                Succeeded=true,
            };
        }

        public async Task<Response<RoleModel>> GetRoleByIdAsync(string roleId)
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var role =roles.Where(r=>r.Id == roleId).FirstOrDefault();

            var roleMap = role.Map();
            return new Response<RoleModel>
            {
                Data = roleMap,
                Code = "00",
                Message = "",
                Succeeded = true,
            };
        }
    }
}
