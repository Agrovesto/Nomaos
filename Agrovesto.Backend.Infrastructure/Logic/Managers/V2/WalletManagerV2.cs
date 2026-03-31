using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public class WalletManagerV2
    {
        private readonly IWalletServiceFactory _factory;
        public WalletManagerV2(IWalletServiceFactory factory)
        {
            _factory = factory;
        }

        public async Task<WalletResponseService<MomoPreApprovalResponse>> GetPreApproval(
                 WalletProviderType provider,
         MomoPreApprovalRequest request,
         WalletProviderParameterModel[] parameters,
         string env)
        {
            var service = _factory.GetService(provider);

            // Check if the service implements the optional interface
            if (service is IMTNMomo registrationProvider)
            {
                // Call the provider-specific method
                var sendRequest = await registrationProvider.GetPreApproval(request, parameters, env);
                //if send request response status contains "APPROVED", then update the wallet transaction to approved
                return sendRequest;
            }

            // Feature not supported by this provider
            return WalletResponseService<MomoPreApprovalResponse>.Failed("User registration not supported by this provider");

        }
        public async Task<WalletResponseService<MomoPreApprovalResponse>> RequestDirectDebitApproval(
            WalletProviderType provider,
    MomoPreApprovalRequest request,
    WalletProviderParameterModel[] parameters,
    string env)
        {
            var service = _factory.GetService(provider);

            // Check if the service implements the optional interface
            if (service is IMTNMomo registrationProvider)
            {
                // Call the provider-specific method
                var sendRequest= await registrationProvider.RequestDirectDebitApproval(request, parameters, env);
                if (sendRequest.Data)
                {
                    // return "Processing" status with the same data if the request was sent successfully

                    //then use background service to call the get pre approval method to check if the user has approved the direct debit
                    //request and update the status accordingly
                }
            }

            // Feature not supported by this provider
            return WalletResponseService<MomoPreApprovalResponse>.Failed("User registration not supported by this provider");

        }
        public async Task<WalletResponseService<PaymentResponse>> CollectAsync(
       WalletProviderType provider,
       CollectionRequest request,
        WalletProviderParameterModel[] parameters,
         string env)
        {
            var service = _factory.GetService(provider);

            var sendRequest= await service.CollectAsync(request,parameters,env);
           // if (sendRequest is successful)
            {
                // return "Processing" status with the same data if the request was sent successfully

                //then use background service to call the get requesttopay method to check if debit was successful or not and update the status accordingly
                //request and update the status accordingly

                //else, use another endpoint to check the status of the transaction and update the status accordingly
            }

            return sendRequest;
        }
    }
}
