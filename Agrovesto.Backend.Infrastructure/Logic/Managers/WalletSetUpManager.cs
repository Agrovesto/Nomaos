using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Microsoft.VisualBasic;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public interface IWalletSetUpManager
    {
        Task<(bool status, string message)> CreateWalletMultipleAsync(ListQueryParams args);
        Task<(bool status, string message)> CreateWalletSingleAsync(ListQueryParams args);
        Task<ResponseDto> GetUserWalletAsync(ListQueryParams query);
    }

    public class WalletSetUpManager: IWalletSetUpManager
    {
        private readonly IWalletService _walletService;
        private readonly IWalletManager _walletManager;
        private readonly IFarmerRepository _farmerRepository;
        private readonly IBusinessRepository _businessRepository;

        private readonly IWalletProviderRepository _providerRepo;
        private readonly IOrganizationConfigurationRepository _orgConfig;
        public WalletSetUpManager(IBusinessRepository businessRepository, IFarmerRepository farmerRepository,
            IWalletManager walletManager,IOrganizationConfigurationRepository orgConfig,
            IWalletService walletService, IWalletProviderRepository providerRepo)
        {
            _walletService = walletService;
            _providerRepo = providerRepo;
            _orgConfig = orgConfig;
            _walletManager = walletManager;
            _farmerRepository = farmerRepository;
            _businessRepository = businessRepository;
        }
        public async Task<WalletProviderParameterModel[]> RetrieveParams(string WalletProviderCode,string Env, string businessCode)
        {
            //var response = new CafeOneParams();
            var provider = await _providerRepo.GetProvidersForBusiness(businessCode);
            var providerId = provider.Where(x => x.WalletProvider.Code == WalletProviderCode).FirstOrDefault().ProviderId;
            WalletProviderParameterModel[] parameters = await _providerRepo.GetParametersByProvider(providerId, Env);

            return parameters;

        }
      

        #region multiple
        public async Task<(bool status, string message)> CreateWalletMultipleAsync(ListQueryParams args)
        {
            try
            {
                // Validate business
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                if (business == null)
                    return (false, $"Business with code {args.businessCode} does not exist");

                // Validate organization
                var orgConfig = await _orgConfig.GetByCodeAsync(args.organizationCode);
                if (orgConfig == null)
                    return (false, $"Organization {args.organizationCode} not found");

                // Resolve wallet provider
                var walletProviderCode = ResolveWalletProvider(orgConfig, args.SelectedProviderCode);
                var providerParams = await RetrieveParams(walletProviderCode, args.env, args.businessCode);
                var users = await _farmerRepository.GetFarmersByBusinessCode(args.businessCode, args);
                if (users.Count < 0)
                    return (false, $"Organization {args.organizationCode} has no user");

                var results = new List<string>();

                foreach (var user in users)
                {
                    var existingWallet = await _walletManager.GetUserWalletByProviderAsync(user.UserId, walletProviderCode);
                    if (existingWallet != null)
                    {
                        results.Add($"{user.UserId}: Wallet already exists, skipped.");
                        continue; // skip this user
                    }
                    var (success, message) = await CreateWalletForUserAsync(user.UserId, business, walletProviderCode, providerParams, args);
                    results.Add($"{user.UserId}: {message}");
                }

                var failed = results.Where(r => r.Contains("failed", StringComparison.OrdinalIgnoreCase)).ToList();

                if (failed.Any())
                {
                    return (false, $"Some wallets failed to create: {string.Join("; ", failed)}");
                }

                return (true, "All wallets created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error creating wallets: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines which wallet provider to use.
        /// </summary>
        private string ResolveWalletProvider(OrganizationConfigurationModel orgConfig, string selectedProviderCode)
        {
            // 1️⃣ Prefer explicit provider selected by user
            if (!string.IsNullOrEmpty(selectedProviderCode))
                return selectedProviderCode;

            // 2️⃣ Try to get default provider from organization's configurations
            var defaultProvider = orgConfig.WalletProviderConfiguration?
                .FirstOrDefault(p => p.DefaultWalletProvider != null);

            if (!string.IsNullOrEmpty(defaultProvider.DefaultWalletProvider))
                return defaultProvider.DefaultWalletProvider;

        

            // 4️⃣ Fallback to a global constant
            return Providers.FCMBWallet;
        }

        /// <summary>
        /// Creates wallet for a single user.
        /// </summary>
        private async Task<(bool status, string message)> CreateWalletForUserAsync(
            string userId,
            BusinessModel business,
            string walletProviderCode,
            WalletProviderParameterModel[] providerParams,
            ListQueryParams args)
        {
            var getUserCred = await _walletManager.GetSubAccount(userId);
            if (getUserCred == null)
                return (false, "User not found");

            var userModel = getUserCred;

            var walletRequest = new WalletsRequest
            {
                Email = !string.IsNullOrEmpty(userModel.Email)
                    ? userModel.Email
                    : GenerateEmail(userModel.FirstName, business.BusinessEmail),
                FirstName = userModel.FirstName,
                LastName = userModel.LastName,
                PhoneNumber = userModel.PhoneNumber
            };
            providerParams = await RetrieveParams(walletProviderCode, args.env, args.businessCode);
            switch (walletProviderCode)
            {
                case Providers.FCMBWallet:
                    return await HandleFcmbWalletCreation(userId, args, walletRequest, providerParams);
                case Providers.SterlingWallet:
                    return await HandleSterlingWalletCreation(userId, args, walletRequest, providerParams);
                case Providers.MTNMOMO:
                    return(false, "MOMO Wallet provider not configuration not available yet, contact administrator");
                case Providers.ETRANSACT:
                    return (false, "MOMO Wallet provider not configuration not available yet, contact administrator");
                default:
                    return await HandleFcmbWalletCreation(userId, args, walletRequest, providerParams); // fallback
            }
        }

        /// <summary>
        /// Handles wallet creation for Sterling provider.
        /// </summary>
        private async Task<(bool status, string message)> HandleSterlingWalletCreation(
            string userId,
            ListQueryParams args,
            WalletsRequest request,
            WalletProviderParameterModel[] providerParams
           )
        {
            var accouuntModel = new SubAccountModel
            {
                PhoneNumber=request.PhoneNumber,
                FirstName=request.FirstName,
                LastName=request.LastName,
                MiddleName= request.MiddleName,
                DOB=request.DOB,
                Bvn=request.BVN,
                Email=request.Email,
                NIN=request.NIN,
                Address=request.Address,
                Alias=request.Alias,
                City=request.City
            };
           // var response = await _walletService.CreateWallet(request, providerParams, args.Env);
            var response = await _walletManager.UpdateUserSterlingWalletProfile(accouuntModel, new ParameterArgs { Env=args.env, UserId=userId});

            if (response == null)
                return (false, "Wallet creation failed, try again later!");

            //if (response.status != "SUCCESS")
            //    return (false, response.message ?? "Wallet creation failed");

            //var account = response.data.SuccessfulVirtualAccounts.FirstOrDefault();
            //if (account == null)
            //    return (false, "No virtual account returned from wallet service");

            var userWallet = new WalletModel
            {
                AccountName = response.SubAccountName,
                AccountNumber = response.BankAccount,
                BankName = Providers.FCMBWallet,
                WalletProviderCode = Providers.FCMBWallet,
                OrganizationCode = args.organizationCode,
                UserId = userId,
                CreatedBy = args.UserId
            };

            await _walletManager.CreateUserWallet(userWallet);

            return (true, "Wallet created successfully");
        }


        /// <summary>
        /// Handles wallet creation for FCMB provider.
        /// </summary>
        private async Task<(bool status, string message)> HandleFcmbWalletCreation(
            string userId,
            ListQueryParams args,
            WalletsRequest request,
            WalletProviderParameterModel[] providerParams
           )
        {
            var response = await _walletService.CreateWallet(request, providerParams, args.env);

            if (response == null)
                return (false, "No response from wallet service");

            if (response.status != "SUCCESS")
                return (false, response.message ?? "Wallet creation failed");

            var account = response.data.SuccessfulVirtualAccounts.FirstOrDefault();
            if (account == null)
                return (false, "No virtual account returned from wallet service");

            var userWallet = new WalletModel
            {
                AccountName = account.VirtualAccountName,
                AccountNumber = account.VirtualAccountNumber,
                BankName = Providers.FCMBWallet,
                WalletProviderCode = Providers.FCMBWallet,
                OrganizationCode = args.organizationCode,
                UserId = userId,
                CreatedBy = args.UserId
            };

            await _walletManager.CreateUserWallet(userWallet);

            return (true, "Wallet created successfully");
        }
        #endregion multiple
        #region single
        public async Task<(bool status, string message)> CreateWalletSingleAsync(ListQueryParams args)
        {
            try
            {
                // 1. Validate business
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                if (business == null)
                    return (false, $"Business with code {args.businessCode} does not exist");

                // 2. Validate user
                var getUserCred = await _walletManager.GetSubAccount(args.UserId);
                if (getUserCred  == null)
                    return (false, "User not found");

                var userModel = getUserCred;

                // 3. Resolve wallet provider
                var walletProviderCode = await ResolveWalletProviderAsync(args);
                var existingWallet = await _walletManager.GetUserWalletByProviderAsync(args.UserId, walletProviderCode);
                if (existingWallet != null)
                {
                    return (true, $"User Wallet Exist for {walletProviderCode}");
                }
                // 4. Retrieve provider parameters
                var providerParams = await RetrieveParams(walletProviderCode, args.env, args.businessCode);

                // 5. Build request payload
                var walletRequest = new WalletsRequest
                {
                    Email = !string.IsNullOrEmpty(userModel.Email)
                        ? userModel.Email
                        : GenerateEmail(userModel.FirstName, business.BusinessEmail),
                    FirstName = userModel.FirstName,
                    LastName = userModel.LastName,
                    PhoneNumber = userModel.PhoneNumber
                };

                // 6. Process wallet creation based on provider
                return walletProviderCode switch
                {
                    Providers.FCMBWallet => await HandleFcmbWalletCreation(userModel.UserId, args, walletRequest, providerParams),
                    Providers.SterlingWallet => (false, "SterlingWallet provider not implemented yet"),
                    _ => await HandleFcmbWalletCreation(userModel.UserId, args, walletRequest, providerParams) // fallback
                };
            }
            catch (Exception ex)
            {
                return (false, $"Error creating wallet: {ex.Message}");
            }
        }
        public async Task<ResponseDto> GetUserWalletAsync(ListQueryParams query)
        {
            var walletProviderCode = await ResolveWalletProviderAsync(query);
            var existingWallet = await _walletManager.GetUserWalletByProviderAsync(query.UserId, walletProviderCode);
            if (existingWallet == null)
            {
                return ResponseDto.Failure($"User Wallet does not Exist");
            }
            return ResponseDto.Success("Wallet Retrieved Successfully.", new
            {
                existingWallet.UserId,
                existingWallet.Id,
                existingWallet.AccountName,
                existingWallet.AccountNumber,
                existingWallet.BankName,
                existingWallet.WalletBalance,
                existingWallet.WalletProviderCode
            });
        }

        /// <summary>
        /// Resolves which wallet provider should be used based on organization and user args.
        /// </summary>
        private async Task<string> ResolveWalletProviderAsync(ListQueryParams args)
        {
            if (!string.IsNullOrEmpty(args.organizationCode))
            {
                var orgConfig = await _orgConfig.GetByCodeAsync(args.organizationCode);
                if (orgConfig == null)
                    throw new Exception($"Organization {args.organizationCode} not found");


                var defaultProvider = orgConfig.WalletProviderConfiguration?
               .FirstOrDefault(p => p.DefaultWalletProvider != null);

                if (!string.IsNullOrEmpty(defaultProvider.DefaultWalletProvider))
                    return defaultProvider.DefaultWalletProvider;


                if (!string.IsNullOrEmpty(args.SelectedProviderCode))
                    return args.SelectedProviderCode;
            }

            // Fallback
            return !string.IsNullOrEmpty(args.SelectedProviderCode)
                ? args.SelectedProviderCode
                : Providers.FCMBWallet;
        }

      
        #endregion single

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
