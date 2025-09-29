using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface IWalletService<T,R>
    {
        //Task<(R data, bool status, string message)> GetToken(T model, string env);
        Task<R> GetBanks(T model, WalletProviderParameterModel[] parameters, string env);
        Task<R> CreateWallet(T model, WalletProviderParameterModel[] parameters, string env);
        Task<R> NameEnquiry(T model, WalletProviderParameterModel[] parameters, string env);
        Task<R> GetWalletDetails(T model,WalletProviderParameterModel[] parameters, string env);
        Task<R> InitiateFundTransfer(T model, WalletProviderParameterModel[] parameters, string env);
        Task<R> FetchTransferStatus(T model,WalletProviderParameterModel[] parameters, string env);
        Task<R> GetAllWallets(T model, WalletProviderParameterModel[] parameters, string env);
    }

    public interface IRequestMapper
    {
        object MapToWalletData<T>(T model);
    }

}
