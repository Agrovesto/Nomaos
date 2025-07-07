using Agrovesto.Backend.Core.Models.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IPaymentProviderManager
    {
        Task<PaymentProviderModel> CreateProvider(PaymentProviderModel model);
        Task<PaymentProviderModel> UpdateProvider(PaymentProviderModel model);
        Task<PaymentProviderModel> UpdateProviderParameter(PaymentProviderModel model);
        Task<PaymentProviderModel[]> Get();
        Task<PaymentProviderModel> Get(long providerId);
        Task<PaymentProviderModel> GetByName(string name);
        Task<PaymentProviderModel> Get(string providerCode);
        Task<bool> DisableProvider(long id);
        Task<bool> EnableProvider(long id);
        Task<ProviderParameterModel[]> GetParametersByProvider(long providerId, string Env);

    }
}
