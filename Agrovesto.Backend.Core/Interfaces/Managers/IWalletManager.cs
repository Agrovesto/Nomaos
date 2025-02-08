using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IWalletManager
    {
        Task<SubAccountModel> UpdateBusinessProfile(SubAccountModel model, ParameterArgs args);
        Task<SubAccountModel> UpdateUserProfile(SubAccountModel model, ParameterArgs args);
    }
}
