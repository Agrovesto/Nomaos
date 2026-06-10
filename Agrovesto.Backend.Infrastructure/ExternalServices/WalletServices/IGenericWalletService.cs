using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Core.V2.Onboarding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{
  
    public interface IGenericWalletService
    {
        bool RequiresPreDebit { get; }
        // Create wallet/account with provider
        Task<WalletResponseService<object>> CreateWalletAsync(CreateWalletRequest request, WalletProviderParameterModel[] parameters, string Env);

        // Collect money from user (RequestToPay)
        Task<WalletResponseService<PaymentResponse>> InitiateWalletTopUpAsync(WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string Env);

        // Send money to user
        Task<WalletResponseService<PaymentResponse>> InitiateWalletTransferAsync(TransferRequest request, WalletProviderParameterModel[] parameters, string Env);

        // Check transaction status
        Task<WalletResponseService<PaymentStatusResponse>> GetTransactionStatusAsync(string reference, WalletProviderParameterModel[] parameters, string Env);

        // Validate user account
        Task<WalletResponseService<AccountValidationResponse>> ValidateAccountAsync(UserProfileResponse data, WalletProviderParameterModel[] parameters, string Env);
        }

}
