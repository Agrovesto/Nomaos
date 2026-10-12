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
        // ===================== Global provider catalog (unchanged) =====================
        Task<WalletProviderModel> ChangeStatus(long id, ProviderStatus providerStatus);
        Task<WalletProviderModel> CreateProvider(WalletProviderModel model);
        Task<WalletProviderModel> Get(string providerCode);
        Task<WalletProviderModel> GetById(long providerId);
        Task<WalletProviderModel[]> GetAll();
        Task<WalletProviderModel[]> GetActiveProvider();
        Task<WalletProviderModel> GetByName(string name);
        Task<WalletProviderModel> UpdateProvider(WalletProviderModel model);
        Task<WalletProviderModel> UpdateProviderParameter(WalletProviderModel model);
        Task<WalletProviderParameterModel> GetParametersById(long parameterId);
        Task<WalletProviderParameterModel> EditParameterValue(WalletProviderParameterModel model);

        // Kept with original signature for backward compatibility — returns GLOBAL defaults only,
        // no business override applied. Existing callers keep working unchanged.
        Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string env);

        // ===================== Business-scoped (multi-tenant) layer — new =====================

        /// <summary>Providers a specific business has enabled.</summary>
        Task<BusinessWalletProviderModel[]> GetProvidersForBusiness(string businessCode);

        /// <summary>Enables a provider for a business, optionally with tenant-specific credential overrides.</summary>
        Task<BusinessWalletProviderModel> EnableProviderForBusiness(
            string businessCode, long providerId, WalletProviderParameterModel[] overrideParameters, bool isDefault = false);

        /// <summary>Disables (soft) a provider previously enabled for a business.</summary>
        Task<bool> DisableProviderForBusiness(string businessCode, long providerId);

        /// <summary>
        /// Resolves EFFECTIVE parameters for a business calling a provider: global defaults merged
        /// with business-level overrides (business value wins on matching Key). Use this one at
        /// runtime when actually calling out to the provider on behalf of a tenant.
        /// </summary>
        Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string businessCode, string env);

        Task<BusinessWalletProviderParameterModel> EditBusinessParameterValue(BusinessWalletProviderParameterModel model);
    }
}
