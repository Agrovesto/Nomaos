using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.VisualBasic;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class WalletSetUpManager<R>
    {
        private readonly IWalletService<UserModel, FCMBWalletResponse<R>> _walletService;

        private readonly IWalletProviderRepository _providerRepo;
        public WalletSetUpManager(IWalletService<UserModel, FCMBWalletResponse<R>> walletService, IWalletProviderRepository providerRepo)
        {
            _walletService = walletService;
            _providerRepo = providerRepo;
        }
        public async Task<WalletProviderParameterModel[]> RetrieveParams(string WalletProviderCode,string Env)
        {
            var response = new CafeOneParams();
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == WalletProviderCode).FirstOrDefault().ProviderId;
            WalletProviderParameterModel[] parameters = await _providerRepo.GetParametersByProvider(providerId, Env);

            return parameters;

        }

      
        public async Task<(bool status, string message)> CreateWallet(UserModel user, BusinessRegistrationArgs args)
        {
            try
            {
                string walletProviderCode;

                // 1. Check if user belongs to an organization
                if (!string.IsNullOrEmpty(user.OrganizationCode))
                {
                    // Retrieve organization configuration
                    var orgConfig = new
                    {
                        DefaultWalletProvider= "FCMB"
                    };//await _providerRepo.GetOrganizationConfigAsync(user.OrganizationCode);

                    if (!string.IsNullOrEmpty(orgConfig?.DefaultWalletProvider))
                    {
                        // Use organization's default provider
                        walletProviderCode = orgConfig.DefaultWalletProvider;
                    }
                    else if (!string.IsNullOrEmpty(args.WalletProviderCode))
                    {
                        // Organization has no default, use user-selected provider
                        walletProviderCode = args.WalletProviderCode;
                    }
                    else
                    {
                        // Fallback to default provider
                        walletProviderCode = Providers.FCMBWallet;
                    }
                }
                else
                {
                    // User not in org, use user-selected provider or fallback to default
                    walletProviderCode = !string.IsNullOrEmpty(args.WalletProviderCode)
                        ? args.WalletProviderCode
                        : Providers.FCMBWallet;
                }

                // Retrieve provider parameters
                var retrieveParams = await RetrieveParams(walletProviderCode, args.Env);

                // Call wallet service dynamically based on selected provider
                FCMBWalletResponse<R> createWalletResponse;

                switch (walletProviderCode)
                {
                    case Providers.FCMBWallet:
                        createWalletResponse = await _walletService.CreateWallet(user, retrieveParams, args.Env);
                        break;

                    case Providers.SterlingWallet:
                        // TODO: Implement BankBWallet logic
                        return (false, "BankBWallet provider not implemented yet");

                    default:
                        // Fallback to BankAWallet if provider code is unrecognized
                        createWalletResponse = await _walletService.CreateWallet(user, retrieveParams, args.Env);
                        break;
                }

                if (createWalletResponse != null && createWalletResponse.status == "SUCCESS")
                    return (true, "Wallet created successfully");

                return (false, createWalletResponse?.message ?? "Wallet creation failed");
            }
            catch (Exception ex)
            {
               // _log.LogError(ex, "Error creating wallet for user {Email}", user.Email);
                return (false, $"Error creating wallet: {ex.Message}");
            }
        }

    }
}
