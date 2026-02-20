using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{
  
    public interface IGenericWalletService
    {
        // Create wallet/account with provider
        Task<WalletResponseService<object>> CreateWalletAsync(CreateWalletRequest request, WalletProviderParameterModel[] parameters, string Env);

        // Collect money from user (RequestToPay)
        Task<WalletResponseService<PaymentResponse>> CollectAsync(CollectionRequest request, WalletProviderParameterModel[] parameters, string Env);

        // Send money to user
        Task<WalletResponseService<PaymentResponse>> DisburseAsync(DisbursementRequest request, WalletProviderParameterModel[] parameters, string Env);

        // Check transaction status
        Task<WalletResponseService<PaymentStatusResponse>> GetTransactionStatusAsync(string reference, WalletProviderParameterModel[] parameters, string Env);

        // Validate user account
        Task<WalletResponseService<AccountValidationResponse>> ValidateAccountAsync(string accountNumber, WalletProviderParameterModel[] parameters, string Env);
    }

}
