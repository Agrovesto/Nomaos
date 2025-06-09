using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IWalletManager
    {
        Task<SubAccountModel> CreateSubAccount(SubAccountModel model);
        Task<SubAccountModel> GetSubAccount(string userId);
        Task<SubAccountModel> GetSubAccountByUserId(string userId);
        Task<SterlingBankWalletModel> GetUserSterlingAccount(string userId);
        Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> PayoutTransferFromUserAccountToAgrovestoAccount(PostWalletToWalletTransferRequestModel request, string env);
        Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> PayoutTransferFromUserAccountToBusinessAccount(PostWalletToWalletTransferRequestModel request, string env);
        Task<CafeOneCustomerWalletResponse> RetrieveCustomerWallet(string userId, string env);
        Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> TransferFromUserAccountToAgrovestoAccount(WalletToWalletTransferRequestModel request, string env);
        Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> TransferFromUserAccountToBusinessAccount(WalletToWalletTransferRequestModel request, string env);
        Task<SubAccountModel> UpdateBusinessProfile(SubAccountModel model, ParameterArgs args);
        Task<SubAccountModel> UpdateUserProfile(SubAccountModel model, ParameterArgs args);
    }
}
