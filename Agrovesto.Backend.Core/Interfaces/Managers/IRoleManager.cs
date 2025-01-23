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
        Task<AgrovestoResponse<RoleModel>> CreateRoleAsync(RoleModel model);
    }
}
