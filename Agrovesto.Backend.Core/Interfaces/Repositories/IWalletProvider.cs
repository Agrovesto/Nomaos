using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IWalletProviderRepository
    {
        Task<WalletProviderModel> ChangeStatus(long id, ProviderStatus providerStatus);
        Task<WalletProviderModel> CreateProvider(WalletProviderModel model);
        Task<WalletProviderModel> Get(string providerCode);
        Task<WalletProviderModel> Get(long providerId);
        Task<WalletProviderModel[]> Get();
        Task<WalletProviderModel[]> GetActiveProvider();
        Task<WalletProviderModel> GetByName(string name);
        Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string Env);
        Task<WalletProviderModel> UpdateProvider(WalletProviderModel model);
        Task<WalletProviderModel> UpdateProviderParameter(WalletProviderModel model);
    }
}
