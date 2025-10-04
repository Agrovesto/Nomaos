using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IWalletRepository
    {
        Task<SterlingBankWalletModel> CreateSterlingWallet(SterlingBankWalletModel model);
        Task<SubAccountModel> CreateSubAccount(SubAccountModel model);
        Task<WalletModel> CreateUserWallet(WalletModel model);
        Task<SterlingBankWalletModel> GetSterlingWallet(string agrovestoUserId);
        Task<SterlingBankWalletModel[]> GetSterlingWallets();
        Task<SubAccountModel> GetSubAccount(string userId);
        Task<WalletModel> GetUserWalletByProviderAsync(string userId, string walletProviderCode);
        Task<SubAccountModel> UpdateSubAccount(SubAccountModel model);
    }
}
