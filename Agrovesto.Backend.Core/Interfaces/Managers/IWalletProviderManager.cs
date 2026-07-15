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
        // ===================== Global provider catalog (unchanged) =====================
        Task<bool> DisableProvider(long id);
        Task<bool> EnableProvider(long id);
        Task<WalletProviderModel> CreateProvider(WalletProviderModel model);
        Task<WalletProviderModel[]> GetAll();
        Task<WalletProviderModel> Get(long providerId);
        Task<WalletProviderModel> Get(string providerCode);
        Task<WalletProviderModel> GetByName(string name);
        Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string env);
        Task<WalletProviderParameterModel> GetParametersById(long parameterId);
        Task<WalletProviderModel> UpdateProvider(WalletProviderModel model);
        Task<WalletProviderModel> UpdateProviderParameter(WalletProviderModel model);
        Task<WalletProviderParameterModel> EditParameterValue(WalletProviderParameterModel model);

        // ===================== Business-scoped (multi-tenant) layer — new =====================

        /// <summary>Providers a specific business has enabled.</summary>
        Task<BusinessWalletProviderModel[]> GetProvidersForBusiness(string businessCode);

        /// <summary>Enables a provider for a business, optionally with tenant-specific credential overrides.</summary>
        Task<BusinessWalletProviderModel> EnableProviderForBusiness(
            string businessCode, long providerId, WalletProviderParameterModel[] overrideParameters, bool isDefault = false);

        /// <summary>Disables a provider previously enabled for a business.</summary>
        Task<bool> DisableProviderForBusiness(string businessCode, long providerId);

        /// <summary>
        /// Effective parameters for a business calling a provider — global defaults merged with
        /// business-level overrides. Use this at runtime when actually invoking the provider.
        /// </summary>
        Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string businessCode, string env);

        Task<BusinessWalletProviderParameterModel> EditBusinessParameterValue(BusinessWalletProviderParameterModel model);
    }
}
