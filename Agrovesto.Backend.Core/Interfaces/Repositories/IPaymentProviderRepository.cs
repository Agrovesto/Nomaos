using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProviderStatus = Agrovesto.Backend.Core.Models.Payment.ProviderStatus;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IPaymentProviderRepository
    {
        Task<PaymentProviderModel> CreateProvider(PaymentProviderModel model);
        Task<PaymentProviderModel> UpdateProvider(PaymentProviderModel model);
        Task<PaymentProviderModel> UpdateProviderParameter(PaymentProviderModel model);
        Task<PaymentProviderModel[]> Get();
        Task<PaymentProviderModel> Get(long providerId);
        Task<PaymentProviderModel> GetByName(string name);
        Task<PaymentProviderModel> Get(string providerCode);
        Task<PaymentProviderModel> ChangeStatus(long id, ProviderStatus providerStatus);
        Task<ProviderParameterModel[]> GetParametersByProvider(long providerId, string Env);
        Task<PaymentProviderModel[]> GetActiveProvider();
    }

}
