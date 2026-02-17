using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.Infrastructure.V2.Internal
{
    public interface INomaOSWalletService
    {
        // Define methods that the NomaOSWalletService should implement
    }
    public class NomaOSWalletService : INomaOSWalletService
    {
        private readonly IWalletServiceFactory _factory;
        private readonly IBusinessRepository _businessRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWalletProviderRepository _providerRepo;
        public NomaOSWalletService(IBusinessRepository businessRepository, 
            IOnboardingRepository onboardingRepository,
            IWalletProviderRepository providerRepo, IWalletServiceFactory factory)
        {
            _factory = factory;
            _providerRepo = providerRepo;
            _businessRepository = businessRepository;
            _onboardingRepository = onboardingRepository;
        }
        public async Task<WalletProviderParameterModel[]> RetrieveParams(string WalletProviderCode, string Env)
        {
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == WalletProviderCode).FirstOrDefault().ProviderId;
            WalletProviderParameterModel[] parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            return parameters;

        }



        public async Task<(bool status, string message)> CreateWallet(WalletQueryParams args)
        {
            try
            {
                // 1. Validate business
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                if (business == null)
                    return (false, $"Business with code {args.businessCode} does not exist");

                // 2. Validate user
                var getUserCred = await _onboardingRepository.GetUserProfile(args.UserId);
                if (getUserCred == null)
                    return (false, "User not found");

                var userModel = getUserCred;

                var providerParams = await RetrieveParams(args.WalletProviderCode, args.env);
                var provderEnum = args.WalletProviderCode.ToEnum<WalletProviderType>();

                var service = _factory.GetService(provderEnum);

                //for momo wallet
                var validateOrCreateWallet = await service.ValidateAccountAsync(userModel.Phone, providerParams, args.env);
                
                if (validateOrCreateWallet !=null && validateOrCreateWallet.Data)
                {
                    //call userwallet repository to check if wallet exists for user and provider, if exist, update it, if not create
                    return (true, "Wallet already exists");

                }
                return (false, "Wallet does not exist and creation process is not implemented yet");
                // 6. Process wallet creation based on provider
                //return provderEnum switch
                //{
                //    WalletProviderType.Momo:
                //    await service.RequestDirectDebitApproval(request, parameters, env);

                //    Providers.FCMBWallet => await HandleFcmbWalletCreation(userModel.UserId, args, walletRequest, providerParams),
                //    Providers.SterlingWallet => (false, "SterlingWallet provider not implemented yet"),
                //    _ => await HandleFcmbWalletCreation(userModel.UserId, args, walletRequest, providerParams) // fallback
                //};
            }
            catch (Exception ex)
            {
                return (false, $"Error creating wallet: {ex.Message}");
            }
        }
        private static string ExtractDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return string.Empty;
            }
            string[] parts = email.Split('@');
            return parts.Length == 2 ? parts[1] : string.Empty;
        }
        private static string GenerateEmail(string fullName, string domain)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentException("Invalid input provided.");
            }
            domain = ExtractDomain(domain);
            fullName = Regex.Replace(fullName.Trim(), @"[^a-zA-Z\s]", "");

            string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length == 0)
            {
                throw new ArgumentException("Name must contain at least one word.");
            }

            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? nameParts[^1] : "user";

            firstName = firstName.ToLower();
            lastName = lastName.ToLower();

            return $"{firstName}.{lastName}@{domain}";
        }
    }
}
