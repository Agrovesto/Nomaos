using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
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
        Task<WalletResponseService<object>> CreateWalletAsync(CreateWalletRequest request);

        // Collect money from user (RequestToPay)
        Task<WalletResponseService<PaymentResponse>> CollectAsync(CollectionRequest request);

        // Send money to user
        Task<WalletResponseService<PaymentResponse>> DisburseAsync(DisbursementRequest request);

        // Check transaction status
        Task<WalletResponseService<PaymentStatusResponse>> GetTransactionStatusAsync(string reference);

        // Validate user account
        Task<WalletResponseService<bool>> ValidateAccountAsync(string accountNumber);
    }

}
