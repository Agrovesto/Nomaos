using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IRoleManager
    {
        Task<bool> AssignRoleToBusinessUserAsync(string userId, string roleName, long businessId);
        Task<AgrovestoResponse<RoleModel>> BusinesCreateRoleAsync(string userId, RoleModel model);
        Task<AgrovestoResponse<RoleModel>> CreateRoleAsync(RoleModel model);
        Task<Response<RoleModel>> GetRoleByIdAsync(string roleId);
        Task<PagedListResponse<List<RoleModel>>> GetRolesAsync();
        Task<PagedListResponse<List<RoleModel>>> GetRolesByBusinessAsync(string userId);
    }
}
