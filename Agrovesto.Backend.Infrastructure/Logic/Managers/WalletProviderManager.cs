using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using SendGrid.Helpers.Errors.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class WalletProviderManager : IWalletProviderManager
    {
        private readonly IWalletProviderRepository _providerRepo;
        public WalletProviderManager(IWalletProviderRepository providerRepository
      )
        {
            _providerRepo = providerRepository;
        }

        // ============================= Global provider catalog =============================

        public async Task<bool> DisableProvider(long id)
        {
            WalletProviderModel model = await _providerRepo.ChangeStatus(id, ProviderStatus.Disabled);
            if (model == null)
            {
                throw new Exception("Request failed. Please try again");
            }

            return model.Status == ProviderStatus.Disabled;
        }

        public async Task<bool> EnableProvider(long id)
        {
            WalletProviderModel model = await _providerRepo.ChangeStatus(id, ProviderStatus.Active);
            if (model == null)
            {
                throw new Exception("Request failed. Please try again");
            }

            return model.Status == ProviderStatus.Active;
        }

        public async Task<WalletProviderModel> CreateProvider(WalletProviderModel model)
        {
            model.Validate();
            var result = await _providerRepo.CreateProvider(model);
            if (result == null)
            {
                throw new Exception("Request failed. Please try again");
            }
            return result;
        }

        public async Task<WalletProviderModel[]> GetAll()
        {
            return await _providerRepo.GetAll();
        }

        public async Task<WalletProviderModel> Get(long providerId)
        {
            var res = await _providerRepo.Get(providerId);
            if (res == null) throw new NotFoundException("Provider has no parameter");
            return res;
        }

        public async Task<WalletProviderModel> Get(string providerCode)
        {
            var res = await _providerRepo.Get(providerCode);
            if (res == null) throw new NotFoundException("Provider has no parameter");
            return res;
        }

        public async Task<WalletProviderModel> GetByName(string name)
        {
            var res = await _providerRepo.GetByName(name);
            if (res == null) throw new NotFoundException("Provider has no parameter");
            return res;
        }

        public async Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string env)
        {
            var res = await _providerRepo.GetParametersByProvider(providerId, env);
            if (res == null) throw new NotFoundException("Provider has no parameter");
            return res;
        }

        public async Task<WalletProviderParameterModel> GetParametersById(long parameterId)
        {
            var res = await _providerRepo.GetParametersById(parameterId);
            if (res == null) throw new NotFoundException("parameter not found");
            return res;
        }

        public async Task<WalletProviderModel> UpdateProvider(WalletProviderModel model)
        {
            var result = await _providerRepo.UpdateProvider(model);
            if (result == null)
            {
                throw new Exception("An error occur while try to update provider. Please try again");
            }

            return result;
        }

        public async Task<WalletProviderModel> UpdateProviderParameter(WalletProviderModel model)
        {
            var result = await _providerRepo.UpdateProviderParameter(model);
            if (result == null)
                throw new Exception("Failed to update parameter");
            return result;
        }

        public async Task<WalletProviderParameterModel> EditParameterValue(WalletProviderParameterModel model)
        {
            var result = await _providerRepo.EditParameterValue(model);
            if (result == null)
                throw new Exception("Failed to update parameter");
            return result;
        }

        // ============================= Business-scoped (multi-tenant) layer =============================

        public async Task<BusinessWalletProviderModel[]> GetProvidersForBusiness(string businessCode)
        {
            if (string.IsNullOrWhiteSpace(businessCode))
                throw new ArgumentException("Business code is required");

            return await _providerRepo.GetProvidersForBusiness(businessCode);
        }

        public async Task<BusinessWalletProviderModel> EnableProviderForBusiness(
            string businessCode, long providerId, WalletProviderParameterModel[] overrideParameters, bool isDefault = false)
        {
            if (string.IsNullOrWhiteSpace(businessCode))
                throw new ArgumentException("Business code is required");

            // Confirms the provider exists and is active before letting a tenant opt into it —
            // repo also checks existence, but this gives a clearer "not available" message and
            // stops a business enabling a Disabled/Suspended provider.
            var provider = await _providerRepo.Get(providerId);
            if (provider == null)
                throw new NotFoundException("Provider can not be found");
            if (provider.Status != ProviderStatus.Active)
                throw new Exception("Provider is not currently available");

            var result = await _providerRepo.EnableProviderForBusiness(businessCode, providerId, overrideParameters, isDefault);
            if (result == null)
            {
                throw new Exception("Request failed. Please try again");
            }
            return result;
        }

        public async Task<bool> DisableProviderForBusiness(string businessCode, long providerId)
        {
            if (string.IsNullOrWhiteSpace(businessCode))
                throw new ArgumentException("Business code is required");

            return await _providerRepo.DisableProviderForBusiness(businessCode, providerId);
        }

        public async Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string businessCode, string env)
        {
            if (string.IsNullOrWhiteSpace(businessCode))
                throw new ArgumentException("Business code is required");

            var res = await _providerRepo.GetParametersByProvider(providerId, businessCode, env);
            if (res == null || res.Length == 0)
                throw new NotFoundException("Provider has no parameter for this business");
            return res;
        }

        public async Task<BusinessWalletProviderParameterModel> EditBusinessParameterValue(BusinessWalletProviderParameterModel model)
        {
            var result = await _providerRepo.EditBusinessParameterValue(model);
            if (result == null)
                throw new Exception("Failed to update parameter");
            return result;
        }
    }
}
