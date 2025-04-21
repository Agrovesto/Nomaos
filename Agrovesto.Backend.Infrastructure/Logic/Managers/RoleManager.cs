using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
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
        private readonly DataContext _context;
        public RoleManager(UserManager<User> userManager, RoleManager<Role> roleManager,
            DataContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        #region business
        public async Task<AgrovestoResponse<RoleModel>> BusinesCreateRoleAsync(string userId, RoleModel model)
        {
            var business = await _context.Businesses.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (business == null) { throw new Exception($"Business not found"); }
            var role = new Role
            {
                Name = model.RoleName,
                NormalizedName = model.RoleName.ToUpper(),
                BusinessId = business.BusinessId,
                Descriptions = model.RoleDescription
            };

             var result=await _roleManager.CreateAsync(role);
            //await _context.AddAsync(role);
            //await _context.SaveChangesAsync();
            //var result = 
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

        public async Task<PagedListResponse<List<RoleModel>>> GetRolesByBusinessAsync(string userId)
        {
            var business = await _context.Businesses.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (business == null) { throw new Exception($"Business not found"); }

            var roles = await _roleManager.Roles.Where(r => r.BusinessId == business.BusinessId).ToListAsync();
            var roleMap = roles.Select(x => x.Map()).ToList();
            return new PagedListResponse<List<RoleModel>>
            {
                Data = roleMap,
                Code = "00",
                Message = "",
                Count = roleMap.Count,
                Succeeded = true,
            };
        }

        public async Task<bool> AssignRoleToBusinessUserAsync(string userId, string roleName, long businessId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var role = await _roleManager.FindByNameAsync(roleName);

            if (user == null || role == null || role.BusinessId != businessId)
                return false;

            var result = await _userManager.AddToRoleAsync(user, role.Name);
            return result.Succeeded;
        }
        #endregion business

        #region globalAdmin
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
        public async Task<bool> AddUserToRole(string userId, string roleId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var role = await _roleManager.FindByIdAsync(roleId);

            if (user == null || role == null)
                return false;

            var result = await _userManager.AddToRoleAsync(user, role.Name);
            return result.Succeeded;
        }
        public async Task<PagedListResponse<List<RoleModel>>> GetBusinessRoles()
        {
            var roles = await _roleManager.Roles.Where(x => x.IsAvailableToBusiness).ToListAsync();

            var roleMap = roles.Select(x => x.Map()).ToList();
            return new PagedListResponse<List<RoleModel>>
            {
                Data = roleMap,
                Code = "00",
                Message = "",
                Count = roleMap.Count,
                Succeeded = true,
            };
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

        #endregion globalAdmin
    }
}
