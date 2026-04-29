using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
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
        Task<FarmerFinancialSummaryDto> GetUserFinancialSummary(UserWalletParams args);
        Task<PagedListResponse<List<FarmerTransactionDto>>> GetUserWalletTransaction(ListQueryParams args);
        Task<PagedListResponse<List<UserWalletResponseModel>>> GetUserWallletsByUserId(UserWalletParams args);
        Task HandleTopUpWebhook(ProviderWebhookRequest request);
        Task<ProviderResponse> TopUpWallet(TopUpWalletRequest request, WalletQueryParams args);
    }
    public class NomaOSWalletService : INomaOSWalletService
    {
        private readonly IWalletServiceFactory _factory;
        private readonly IBusinessRepository _businessRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWalletProviderRepository _providerRepo;
        private readonly IUserWalletRepository _userWalletRepository;
        private readonly DataContext _context;
        public NomaOSWalletService(IBusinessRepository businessRepository,
            IOnboardingRepository onboardingRepository,
            IUserWalletRepository userWalletRepository,
            IWalletProviderRepository providerRepo, IWalletServiceFactory factory,
            DataContext context)
        {
            _factory = factory;
            _providerRepo = providerRepo;
            _businessRepository = businessRepository;
            _onboardingRepository = onboardingRepository;
            _context = context;
            _userWalletRepository = userWalletRepository;
        }
        public async Task<WalletProviderParameterModel[]> RetrieveParams(string WalletProviderCode, string Env)
        {
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == WalletProviderCode).FirstOrDefault().ProviderId;
            WalletProviderParameterModel[] parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            return parameters;

        }
        public async Task<FarmerFinancialSummaryDto> GetUserFinancialSummary(UserWalletParams args)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                if (business == null) throw new BaseException(ExceptionType.NOTFOUND, "Business not found");


                var summary = await _userWalletRepository.GetUserFinancialSummary(args);
                return summary;
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
        public async Task<PagedListResponse<List<UserWalletResponseModel>>> GetUserWallletsByUserId(UserWalletParams args)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                if (business == null) throw new BaseException(ExceptionType.NOTFOUND, "Business not found");


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
        public async Task<PagedListResponse<List<FarmerTransactionDto>>> GetUserWalletTransaction(ListQueryParams args)
        {
            try
            {
                if(args.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid wallet ID");
                var wallet = await _userWalletRepository.GetUserWalletTransaction(args);
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
            if (string.IsNullOrWhiteSpace(request.TransactionPin))
                throw new Exception("Transaction PIN is required");
       
            try
            {
            
                string? businessCode = null;
                if (!string.IsNullOrWhiteSpace(args.businessCode))
                {
                    var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                    if (business == null)
                        return (false, $"Business with code {args.businessCode} does not exist");

                    businessCode = args.businessCode;
                }

                var userProfile = await _onboardingRepository.GetUserProfile(args.UserId);
                if (userProfile == null)
                    return (false, "User not found");

                var contextType = string.IsNullOrWhiteSpace(args.organizationCode)
                    ? WalletContextType.Personal
                    : WalletContextType.Organization;

                var contextReferenceId = contextType == WalletContextType.Personal
                    ? args.UserId
                    : args.organizationCode;

                var providerParams = await RetrieveParams(request.WalletProviderCode, args.env);
                var providerId = providerParams.Select(x => x.ProviderId).FirstOrDefault();

                object existingWallet = await _userWalletRepository.ExistingWalletForProvider(args, contextType, contextReferenceId, providerId);

                if (existingWallet != null)
                    return (false, "Wallet already exists for this provider and context");

                var providerType = request.WalletProviderCode.ToEnum<WalletProviderType>();
                var providerService = _factory.GetService(providerType);

                var providerUserProfile = new Agrovesto.Backend.Core.V2.Onboarding.UserProfileResponse
                {
                    UserId = userProfile.UserId,
                    Email = userProfile.Email,
                    PhoneNumber = !string.IsNullOrEmpty( userProfile.Phone) ? userProfile.Phone : request.PhoneNumber
                };

                var validationResponse = await providerService.ValidateAccountAsync(providerUserProfile, providerParams, args.env);

                if (validationResponse == null || !validationResponse.IsSuccess)
                    return (false, validationResponse?.Message ?? "Account validation failed");

                var walletNumber = validationResponse.Data?.WalletNumber;
                if (string.IsNullOrWhiteSpace(walletNumber))
                    return (false, "Provider did not return a valid wallet number");

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
                    SetAsDefaultForContext = request.IsDefault,
                    TransactionPin = request.TransactionPin
                };

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


        public async Task<ProviderResponse> TopUpWallet(
           TopUpWalletRequest request,
           WalletQueryParams args)
        {
            try
            {
                if (request.Amount <= 0)
                    return new ProviderResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid amount"
                    };

                var wallet = await _userWalletRepository.GetWalletById(new UserWalletParams
                {
                    UserId = args.UserId,
                    businessCode = args.businessCode,
                    Env = args.env,
                    Id = request.UserWalletId
                });

                if (wallet == null)
                    return new ProviderResponse
                    {
                        IsSuccess = false,
                        Message = "Wallet not found"
                    };

                if (!wallet.IsActive)
                    return new ProviderResponse
                    {
                        IsSuccess = false,
                        Message = "Wallet is inactive"
                    };

                var providerParams = await RetrieveParams(request.WalletProviderCode, args.env);
                var providerId = providerParams.Select(x => x.ProviderId).FirstOrDefault();

                if (wallet.WalletProviderId != providerId)
                    return new ProviderResponse
                    {
                        IsSuccess = false,
                        Message = "Wallet provider mismatch"
                    };

                var providerType = request.WalletProviderCode.ToEnum<WalletProviderType>();
                var providerService = _factory.GetService(providerType);

                var transactionReference = await _userWalletRepository.InitiateWalletTopUp(
                    wallet.Id,
                    request.Amount,
                    request.Description ?? "Wallet top-up");

                var providerRequest = new WalletTopUpProviderRequest
                {
                    Amount = request.Amount,
                    Currency = wallet.Currency,
                    WalletNumber = wallet.WalletNumber,
                    TransactionReference = transactionReference,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Message = $"Top-up for wallet {wallet.WalletNumber}"
                };

                var providerResponse = await providerService.InitiateTopUpAsync(
                    providerRequest,
                    providerParams,
                    args.env);

                if (providerResponse == null || !providerResponse.IsSuccess)
                {
                    await _userWalletRepository.CompleteWalletTopUp(
                        transactionReference,
                        false,
                        providerResponse?.Data.ProviderReference,
                        providerResponse?.Message ?? "Provider failed");

                    return new ProviderResponse
                    {
                        IsSuccess = false,
                        Message = providerResponse?.Message ?? "Top-up initiation failed",
                        ProviderReference = providerResponse?.Data.ProviderReference,
                        Data = new
                        {
                            TransactionReference = transactionReference
                        }
                    };
                }

                return new ProviderResponse
                {
                    IsSuccess = true,
                    Message = providerResponse.Message ?? "Top-up initiated successfully",
                    ProviderReference = providerResponse?.Data.ProviderReference,
                    Data = new
                    {
                        TransactionReference = transactionReference,
                        ProviderData = providerResponse.Data
                    }
                };
            }
            catch (Exception ex)
            {
                return new ProviderResponse
                {
                    IsSuccess = false,
                    Message = $"Error topping up wallet: {ex.Message}"
                };
            }
        }

        public async Task HandleTopUpWebhook(ProviderWebhookRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TransactionReference))
                return;

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var transaction = await _userWalletRepository.GetWalletTransactionByRef(
                    request.TransactionReference);

                if (transaction == null)
                    return;

                if (transaction.WalletTransactionStatus == WalletTransactionStatus.Successful ||
                    transaction.WalletTransactionStatus == WalletTransactionStatus.Reversed)
                    return;

                var isSuccessful = request.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
                var isFailed = request.Status.Equals("FAILED", StringComparison.OrdinalIgnoreCase);

                await _userWalletRepository.UpdateTransactionProviderInfo(
                    transaction.TransactionReference,
                    request.ProviderReference,
                    request.Status,
                    request.RawPayload);

                if (isFailed)
                {
                    await _userWalletRepository.UpdateTransactionStatus(
                        transaction.TransactionReference,
                        WalletTransactionStatus.Failed);

                    await dbTransaction.CommitAsync();
                    return;
                }
                
                if (isSuccessful)
                {
                    var wallet = await _userWalletRepository.GetWalletById(new UserWalletParams
                    {
                        UserId = transaction.UserWallet.UserId,
                        Id = transaction.UserWalletId
                    });

                    if (wallet == null)
                        return;

                    var balanceBefore = wallet.WalletBalance;

                    var netAmount = transaction.TransactionAmount
                                    - transaction.TransactionFee
                                    - transaction.CommissionFee;

                    wallet.WalletBalance += netAmount;

                    transaction.BalanceBefore = balanceBefore;
                    transaction.BalanceAfter = wallet.WalletBalance;
                    transaction.WalletTransactionStatus = WalletTransactionStatus.Successful;
                    
                    await _userWalletRepository.UpdateWalletBalance(new UserWallet
                    {
                        WalletBalance= wallet.WalletBalance,
                        IsActive = wallet.IsActive,
                        Id = wallet.Id,

                    });
                    await _userWalletRepository.UpdateTransaction(transaction);

                    await dbTransaction.CommitAsync();
                    return;
                }

                await _userWalletRepository.UpdateTransactionStatus(
                    transaction.TransactionReference,
                    WalletTransactionStatus.Pending);

                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}
