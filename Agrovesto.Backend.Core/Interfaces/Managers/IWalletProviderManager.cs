using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IWalletProviderManager
    {
        Task<WalletProviderModel> CreateProvider(WalletProviderModel model);
        Task<WalletProviderModel> UpdateProvider(WalletProviderModel model);
        Task<WalletProviderModel> UpdateProviderParameter(WalletProviderModel model);
        Task<WalletProviderModel[]> Get();
        Task<WalletProviderModel> Get(long providerId);
        Task<WalletProviderModel> GetByName(string name);
        Task<WalletProviderModel> Get(string providerCode);
        Task<bool> DisableProvider(long id);
        Task<bool> EnableProvider(long id);
        Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string Env);

    }
}
