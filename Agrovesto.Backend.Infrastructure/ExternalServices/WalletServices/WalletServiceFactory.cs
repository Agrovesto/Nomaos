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
        private readonly IEnumerable<IGenericWalletService> _wallets;

        public WalletServiceFactory(IServiceProvider serviceProvider, IEnumerable<IGenericWalletService> wallets)
        {
            _serviceProvider = serviceProvider;
            _wallets = wallets;
        }

        //public IGenericWalletService GetService(WalletProviderType provider)
        //{
        //    return provider switch
        //    {
        //        WalletProviderType.Momo => _serviceProvider.GetRequiredService<MTNMomo>(),
        //        WalletProviderType.Etransact => _serviceProvider.GetRequiredService<EtransactWallet>(),

        //        _ => throw new NotSupportedException($"Provider {provider} not supported")
        //    };
        //}
        public IGenericWalletService GetService(string providerKey)
        {
            var duplicates = _wallets
    .GroupBy(w => w.ProviderKey, StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key);

            if (duplicates.Any())
                throw new InvalidOperationException($"Duplicate wallet provider keys: {string.Join(", ", duplicates)}");
           
            var wallet = _wallets.FirstOrDefault(w =>
                string.Equals(w.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase));

            if (wallet == null)
                throw new NotSupportedException($"Provider '{providerKey}' not supported");

            return wallet;
        }

    }

}
