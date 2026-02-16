using Agrovesto.Backend.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{
    public interface IWalletServiceFactory
    {
        IWalletService GetService(WalletProviderType provider);
    }
    public enum WalletProviderType
    {
        Momo = 0,
        FCMB = 1,
        Sterling = 2,
        Credo = 3,
        CafeOne = 4
    }

}
