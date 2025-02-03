using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface ICafeOneWalletService
    {
        Task<WalletResponseData> AgrovestoToUserWalletTransfer(string accountNumber, decimal amount, string reference, string Env);
        Task<CafeoneDataModel> CreateCustomer(CafeoneCustomerRequest model, string Env);
        Task<WalletResponseData> CreateCustomerWallet(string userId, string WalletName, string Env);
        Task<WalletResponseData> CreateWallet(string WalletName, string Env);
        Task<List<CafeOneBankData>> GetBanks(string env);
        Task<CafeOneWalletProvider> GetUserAccount(string internalAccountId, string env);
        Task<WalletResponseData> GetWallet(string env);
        Task<CafeOneWalletProvider> GetWalletbyId(string walletId, string env);
        Task<AccountResolutionResolutionResponseData> ResolveUserAccount(string accountNumber, string bankCode, string Env);
        Task<WalletResponseData> UserWalletToAgrovestoTransfer(string reference, string ownerId, decimal amount, string fromAccount, string Env);
        Task<CafeOneExternalTransferResponse> UserWalletToExternalBankTransfer(ExternalBankTransferModel extmodel, string Env);

    }
}
