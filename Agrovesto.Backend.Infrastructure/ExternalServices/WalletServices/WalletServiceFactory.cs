using Agrovesto.Backend.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{
    public class WalletServiceFactory : IWalletServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public WalletServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWalletService GetService(WalletProviderType provider)
        {
            return providerType switch
            {
                WalletProviderType.Momo =>
                    _serviceProvider.GetRequiredService<MTNMomo>(),
                WalletProviderType.FCMB =>
              _serviceProvider.GetRequiredService<FCMBWalletService>(),

                WalletProviderType.Sterling =>
                    _serviceProvider.GetRequiredService<SterlingWalletService>(),

                WalletProviderType.Credo =>
                    _serviceProvider.GetRequiredService<CredoWalletService>(),

                WalletProviderType.CafeOne =>
                    _serviceProvider.GetRequiredService<CafeOneWalletService>(),

                _ => throw new NotSupportedException($"Provider {providerType} not supported")
            };
        }
    }

}
