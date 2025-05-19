using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface ICafeOneWalletService
    {
        Task<CafeoneDataModel> CreateCustomer(CafeoneCustomerRequest model, string Env);
        Task<CafeOneCustomerWalletResponse> CreateCustomerWallet(CafeOneCustomerWalletCreationRequest model, string Env);
        Task<CafeoneResponseModel<FundTransferResponse>> FundWithdrawal(FundWithdrawalRequestModel request, string Env);
        Task<List<CafeOneBankData>> GetBanks(string env);
        Task<NameEnquiry> InterBankNameEnquiry(string accountNumber, string bankCode, string env);

        // Task<WalletResponseData> AgrovestoToUserWalletTransfer(string accountNumber, decimal amount, string reference, string Env);
        // Task<WalletResponseData> CreateBusinessUserWallet(string WalletName, string Env, string agrovestoUserId);
        // Task<CafeoneDataModel> CreateCustomer(CafeoneCustomerRequest model, string Env);
        //// Task<WalletResponseData> CreateCustomerWallet(string userId, string WalletName, string Env);
        // Task<CafeOneCustomerWalletResponse> CreateCustomerWallet(CafeOneCustomerWalletCreationRequest model, string Env);
        // Task<WalletResponseData> CreateWallet(string WalletName, string Env);
        // Task<List<CafeOneBankData>> GetBanks(string env);
        // Task<(CafeOneWalletProvider data, string message)> GetUserAccount(string internalAccountId, string env);
        // Task<CafeOneData> GetUserBalance(string walletId, string env);
        // Task<WalletResponseData> GetWallet(string env);
        // Task<CafeOneWalletProvider> GetWalletbyId(string walletId, string env);
        // Task<AccountResolutionResolutionResponseData> ResolveUserAccount(string accountNumber, string bankCode, string Env);
        // Task<CafeOneCustomerWalletResponse> RetrieveCustomerWallet(string customerId, string walletId, string Env);
        // Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> TransferFromUserAccountToAgrovestoAccount(WalletToWalletTransferRequestModel request, string Env);
        // Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> TransferFromUserAccountToBusinessAccount(WalletToWalletTransferRequestModel request, string Env);
        // Task<bool> UpdateCustomerKyc(CafeOneCustomerKYCRequest model, CafeOneParams @params, string env);
        // Task<CafeOnWalletResponse<WalletResponseData>> UserWalletToAgrovestoTransfer(string reference, string ownerId, decimal amount, string fromAccount, string Env);
        // Task<CafeOnWalletResponse<WalletTransferResult>> UserWalletToBusinessWalletTransfer(WalletToWalletTransferRequest request);
        // Task<CafeOneExternalTransferResponse> UserWalletToExternalBankTransfer(ExternalBankTransferModel extmodel, string Env);
        Task<NameEnquiry> IntraBankNameEnquiry(string accountNumber, string env);
        Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> PayoutTransferFromUserAccountToAgrovestoAccount(PostWalletToWalletTransferRequestModel request, string Env);
        Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> PayoutTransferFromUserAccountToBusinessAccount(PostWalletToWalletTransferRequestModel request, string Env);
        Task<CafeOneCustomerWalletResponse> RetrieveCustomerWallet(string customerId, string walletId, string Env);
        Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> TransferFromUserAccountToAgrovestoAccount(WalletToWalletTransferRequestModel request, string Env);
        Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> TransferFromUserAccountToBusinessAccount(WalletToWalletTransferRequestModel request, string Env);
        Task<bool> UpdateCustomerKyc(CafeOneCustomerKYCRequest model, CafeOneParams @params, string env);
    }
}
