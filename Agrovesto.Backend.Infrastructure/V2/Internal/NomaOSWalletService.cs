using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
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
        private readonly IUserWalletRepository _userWalletRepository;
        public NomaOSWalletService(IBusinessRepository businessRepository, 
            IOnboardingRepository onboardingRepository,
            IUserWalletRepository userWalletRepository,
            IWalletProviderRepository providerRepo, IWalletServiceFactory factory)
        {
            _factory = factory;
            _providerRepo = providerRepo;
            _businessRepository = businessRepository;
            _onboardingRepository = onboardingRepository;
            _userWalletRepository = userWalletRepository;
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
            /**
             * 
             * 1. Validate Business
2. Validate User
3. Get Provider Service (Factory)
4. Provider validates account
5. If success → Create wallet
6. Return unified response

             **/
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                if (business == null)
                    return (false, $"Business with code {args.businessCode} does not exist");

                var getUserCred = await _onboardingRepository.GetUserProfile(args.UserId);
                if (getUserCred == null)
                    return (false, "User not found");

                var userModel = getUserCred;

                var providerType = args.WalletProviderCode.ToEnum<WalletProviderType>();
                var providerService = _factory.GetService(providerType);

                var providerParams = await RetrieveParams(args.WalletProviderCode, args.env);
               

                var validationResponse = await providerService.ValidateAccountAsync(userModel.Phone, providerParams, args.env);

                if (validationResponse == null || !validationResponse.IsSucess)
                {
                    return (false, validationResponse?.Message ?? "Account validation failed");
                }

                var walletNumber = validationResponse.Data?.WalletNumber;

                if (string.IsNullOrEmpty(walletNumber))
                    return (false, "Provider did not return a valid wallet number");

                var wallet = new UserWalletModel
                {
                    UserId = args.UserId,
                    WalletBalance = 0,
                    IsDefault = args.IsDefault,
                    WalletNumber = walletNumber,
                    Currency = providerParams
                .FirstOrDefault(x => x.Key == "Currency")?.Value ?? "NGN",
                    WalletProviderId = providerParams
                .Select(x => x.ProviderId)
                .FirstOrDefault()
                };

                  await _userWalletRepository.CreateUserWallet(wallet, new UserWalletParams
                    {
                        businessCode=args.businessCode,
                        Env= args.env,
                        UserId= args.UserId,
                    });



                return (true, "Wallet created successfully");
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
