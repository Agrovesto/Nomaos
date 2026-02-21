using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using DocumentFormat.OpenXml.Spreadsheet;
using Serilog;
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
        Task<(bool status, string message)> CreateWallet(CreateWalletRequest request, WalletQueryParams args);
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
        public async Task<List<UserWalletResponseModel>> GetUserWallletsByUserId(UserWalletParams args)
        {
            try
            {
                    var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                    if(business == null)throw new BaseException(ExceptionType.NOTFOUND, "Business not found");

                
                var wallet = await _userWalletRepository.GetUserWallletsByUserId(args);
                return wallet;
            }
            catch (BaseException ex)
            {
              //  _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<(bool status, string message)> CreateWallet(CreateWalletRequest request, WalletQueryParams args)
        {
            /*
                Flow:
                1. Validate Business (optional)
                2. Validate User
                3. Check existing wallet for same provider + context
                4. Get Provider Service
                5. Validate account with provider
                6. Create wallet entity
                7. Create wallet context (Personal or Organization)
            */

            try
            {
                // 1. Validate Business (optional)
                string? businessCode = null;
                if (!string.IsNullOrWhiteSpace(args.businessCode))
                {
                    var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                    if (business == null)
                        return (false, $"Business with code {args.businessCode} does not exist");

                    businessCode = args.businessCode;
                }

                // 2. Validate User
                var userProfile = await _onboardingRepository.GetUserProfile(args.UserId);
                if (userProfile == null)
                    return (false, "User not found");

                // 3. Determine Context
                var contextType = string.IsNullOrWhiteSpace(args.organizationCode)
                    ? WalletContextType.Personal
                    : WalletContextType.Organization;

                var contextReferenceId = contextType == WalletContextType.Personal
                    ? args.UserId
                    : args.organizationCode;

                // 4. Check for existing wallet with same provider + context
                var providerParams = await RetrieveParams(request.WalletProviderCode, args.env);
                var providerId = providerParams.Select(x => x.ProviderId).FirstOrDefault();

                object existingWallet = await _userWalletRepository.ExistingWalletForProvider(args, contextType, contextReferenceId, providerId);

                if (existingWallet != null)
                    return (false, "Wallet already exists for this provider and context");

                // 5. Get Provider Service
                var providerType = request.WalletProviderCode.ToEnum<WalletProviderType>();
                var providerService = _factory.GetService(providerType);

                var providerUserProfile = new Agrovesto.Backend.Core.V2.Onboarding.UserProfileResponse
                {
                    UserId = userProfile.UserId,
                    Email = userProfile.Email,
                    PhoneNumber = userProfile.Phone
                };

                // 6. Validate Account with Provider
                var validationResponse = await providerService.ValidateAccountAsync(providerUserProfile, providerParams, args.env);

                if (validationResponse == null || !validationResponse.IsSucess)
                    return (false, validationResponse?.Message ?? "Account validation failed");

                var walletNumber = validationResponse.Data?.WalletNumber;
                if (string.IsNullOrWhiteSpace(walletNumber))
                    return (false, "Provider did not return a valid wallet number");

                // 7. Build UserWalletModel for repository
                var walletModel = new UserWalletModel
                {
                    UserId = args.UserId,
                    WalletProviderId = providerId,
                    WalletNumber = walletNumber,
                    WalletBalance = 0,
                    Currency = providerParams.FirstOrDefault(x => x.Key == "Currency")?.Value ?? "NGN",
                    BelongToBusinessCode = businessCode,
                    ContextType = contextType.ToString(),
                    ContextReferenceId = contextReferenceId,
                    SetAsDefaultForContext = request.IsDefault
                };

                // 8. Call repository to create wallet and context
                await _userWalletRepository.CreateUserWallet(walletModel, new UserWalletParams
                {
                    businessCode = businessCode,
                    Env = args.env,
                    UserId = args.UserId
                });

                return (true, "Wallet created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating wallet: {ex.Message}");
            }
        }

      
        /**    public async Task<(bool status, string message)> CreateWallet(WalletQueryParams args)
   {
   
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
          

           // Build a UserProfileResponse expected by wallet provider services
           var userProfile = new Agrovesto.Backend.Core.V2.Onboarding.UserProfileResponse
           {
               UserId = userModel.UserId,
               Email = userModel.Email,
               PhoneNumber = userModel.Phone
           };

           var validationResponse = await providerService.ValidateAccountAsync(userProfile, providerParams, args.env);

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
   }**/
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
