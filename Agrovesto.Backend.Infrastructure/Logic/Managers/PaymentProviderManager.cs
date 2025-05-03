using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class PaymentProviderManager : IPaymentProviderManager
    {
        private readonly IPaymentProviderRepository _providerRepo;
        public PaymentProviderManager(IPaymentProviderRepository providerRepository)
        {
            _providerRepo = providerRepository;
        }
        public async Task<bool> DisableProvider(long id)
        {
            PaymentProviderModel model = await _providerRepo.ChangeStatus(id, ProviderStatus.Disabled);
            if (model == null)
            {
                throw new NotSuccessfulException("Request failed. Please try again");
            }

            return model.Status == ProviderStatus.Disabled;
        }

        public async Task<bool> EnableProvider(long id)
        {
            PaymentProviderModel model = await _providerRepo.ChangeStatus(id, ProviderStatus.Active);
            if (model == null)
            {
                throw new NotSuccessfulException("Request failed. Please try again");
            }

            return model.Status == ProviderStatus.Active;
        }

        public async Task<PaymentProviderModel> CreateProvider(PaymentProviderModel model)
        {
            model.Validate();
            var result = await _providerRepo.CreateProvider(model);
            if (result == null)
            {
                throw new NotSuccessfulException("Request failed. Please try again");
            }
            return result;
        }

        public async Task<PaymentProviderModel[]> Get()
        {
            return await _providerRepo.Get();
        }

        public async Task<PaymentProviderModel> Get(long providerId)
        {
            var res = await _providerRepo.Get(providerId);
            if (res == null) throw new NotFoundException("Provider has no parameter");
            return res;
        }

        public async Task<PaymentProviderModel> Get(string providerCode)
        {
            var res = await _providerRepo.Get(providerCode);
            if (res == null) throw new NotFoundException("Provider has no parameter");
            return res;
        }

        public async Task<PaymentProviderModel> GetByName(string name)
        {
            var res = await _providerRepo.GetByName(name);
            if (res == null) throw new NotFoundException("Provider has no parameter");
            return res;
        }

        public async Task<ProviderParameterModel[]> GetParametersByProvider(long providerId, string Env)
        {
            var res = await _providerRepo.GetParametersByProvider(providerId, Env);
            if (res == null) throw new NotFoundException("Provider has no parameter");
            return res;
        }

        public async Task<PaymentProviderModel> UpdateProvider(PaymentProviderModel model)
        {

            var result = await _providerRepo.UpdateProvider(model);
            if (result == null)
            {
                throw new NotSuccessfulException("An error occur while try to update provider. Please try again");
            }

            return result;
        }

        public async Task<PaymentProviderModel> UpdateProviderParameter(PaymentProviderModel model)
        {
            var result = await _providerRepo.UpdateProviderParameter(model);
            if (result == null)
                throw new NotSuccessfulException("Failed to update parameter");
            return result;
        }
    }
}
