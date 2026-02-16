using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public class WalletManagerV2
    {
        private readonly IWalletServiceFactory _factory;
        public WalletManagerV2(IWalletServiceFactory factory)
        {
            _factory = factory;
        }

        public async Task<WalletResponseService<PaymentResponse>> CollectAsync(
       WalletProviderType provider,
       CollectionRequest request)
        {
            var service = _factory.GetService(provider);

            return await service.CollectAsync(request);
        }
    }
}
