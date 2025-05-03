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

        public async Task<WalletProviderModel[]> Get()
        {
            return await _providerRepo.Get();
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

        public async Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string Env)
        {
            var res = await _providerRepo.GetParametersByProvider(providerId, Env);
            if (res == null) throw new NotFoundException("Provider has no parameter");
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
    }
}
