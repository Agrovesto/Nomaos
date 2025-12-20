using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface IWalletService
    {
        //Task<(R data, bool status, string message)> GetToken(T model, string env);
        Task<BanksResponseData> GetBanks(WalletsRequest model, WalletProviderParameterModel[] parameters, string env);
        Task<WalletsResponse<CreateWalletResponse>> CreateWallet(WalletsRequest model, WalletProviderParameterModel[] parameters, string env);
        Task<CreateWalletResponse> NameEnquiry(WalletsRequest model, WalletProviderParameterModel[] parameters, string env);
        Task<CreateWalletResponse> GetWalletDetails(WalletsRequest model,WalletProviderParameterModel[] parameters, string env);
        Task<CreateWalletResponse> InitiateFundTransfer(WalletsRequest model, WalletProviderParameterModel[] parameters, string env);
        Task<CreateWalletResponse> FetchTransferStatus(WalletsRequest model,WalletProviderParameterModel[] parameters, string env);
        Task<CreateWalletResponse> GetAllWallets(WalletsRequest model, WalletProviderParameterModel[] parameters, string env);
    }

    public interface IRequestMapper
    {
        object MapToWalletData<T>(T model);
    }

}
