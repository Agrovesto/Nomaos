using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Core.Models.Payment;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;
using CreateWalletRequest = Agrovesto.Backend.Core.Models.Businesses.CreateWalletRequest;

namespace Agrovesto.Backend.Infrastructure.V2.Internal
{
    public interface INomaOSWalletService
    {
        // Define methods that the NomaOSWalletService should implement
        Task<(bool status, string message)> CreateWallet(CreateWalletRequest request, WalletQueryParams args);
        Task<bool> GetRequestToPayStatus(string walletProviderCode, string transactionreference, string env,
            string businessCode);
        Task<FarmerFinancialSummaryDto> GetUserFinancialSummary(UserWalletParams args);
        Task<PagedListResponse<List<FarmerTransactionDto>>> GetUserWalletTransaction(ListQueryParams args);
        Task<PagedListResponse<List<UserWalletResponseModel>>> GetUserWallletsByUserId(UserWalletParams args);
        Task<string> HandleTopUpWebhook(ProviderWebhookRequest request);
        Task<ProviderResponse> TopUpWallet(TopUpWalletRequest request, WalletQueryParams args);
        Task UpdateOrganizationInputPackage(WalletQueryParams args, long providerId, UserWalletModel wallet);
        Task<ProviderResponse> WithdrawWallet(WithdrawWalletRequest request, WalletQueryParams args);
    }

            public class NomaOSWalletService : INomaOSWalletService
    {
        private readonly IWalletServiceFactory _factory;
        private readonly IBusinessRepository _businessRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWalletProviderRepository _providerRepo;
        private readonly IUserWalletRepository _userWalletRepository;
        private readonly DataContext _context;
        private readonly IInputFinancingRepository _inputFinancingRepository;
        private readonly IMTNMomo _mtnMomoService;
        public NomaOSWalletService(IBusinessRepository businessRepository,
            IOnboardingRepository onboardingRepository,
            IUserWalletRepository userWalletRepository,
            IWalletProviderRepository providerRepo, IWalletServiceFactory factory,
            DataContext context, IMTNMomo mtnMomoService,
            IInputFinancingRepository inputFinancingRepository)
        {

            _mtnMomoService = mtnMomoService;
            _factory = factory;
            _providerRepo = providerRepo;
            _businessRepository = businessRepository;
            _onboardingRepository = onboardingRepository;
            _context = context;
            _userWalletRepository = userWalletRepository;
            _inputFinancingRepository = inputFinancingRepository;
        }
        public async Task<WalletProviderParameterModel[]> RetrieveParams(string WalletProviderCode, string Env, string businessCode)
        {
            var provider = await _providerRepo.GetProvidersForBusiness(businessCode);
            var providerId = provider.Where(x => x.WalletProvider.Code == WalletProviderCode).FirstOrDefault().ProviderId;
            WalletProviderParameterModel[] parameters = await _providerRepo.GetParametersByProvider(providerId,businessCode, Env);
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

        public async Task<(bool status, string message)> CreateWallet(Core.Models.Businesses.CreateWalletRequest request, WalletQueryParams args)
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

                var userProfile = await _onboardingRepository.GetUseridentity(args.UserId);
                if (userProfile == null)
                    return (false, "User not found");

                var contextType = string.IsNullOrWhiteSpace(args.organizationCode)
                    ? WalletContextType.Personal
                    : WalletContextType.Organization;

                var contextReferenceId = contextType == WalletContextType.Personal
                    ? $"{args.UserId}-{request.WalletProviderCode}"
                    : $"{args.organizationCode}-{request.WalletProviderCode}";

                var providerParams = await RetrieveParams(request.WalletProviderCode, args.env, args.businessCode);
                var providerId = providerParams.Select(x => x.ProviderId).FirstOrDefault();

                object existingWallet = await _userWalletRepository.ExistingWalletForProvider(args, contextType, contextReferenceId, providerId);

                if (existingWallet != null)
                    return (false, "Wallet already exists for this provider and context");

                var providerType = request.WalletProviderCode.ToEnum<WalletProviderType>();
                var providerService = _factory.GetService(request.WalletProviderCode);

                var providerUserProfile = new Agrovesto.Backend.Core.V2.Onboarding.UserProfileResponse
                {
                    UserId = userProfile.UserId,
                    Email = userProfile.Email,
                    PhoneNumber = !string.IsNullOrEmpty( userProfile.Phone) ? userProfile.Phone : request.PhoneNumber,
                    BVN= !string.IsNullOrEmpty(userProfile.BVN) ? userProfile.BVN : request.BVN,
                    NIN= !string.IsNullOrEmpty(userProfile.NIN) ? userProfile.NIN : request.NIN,

                    FirstName= userProfile.Name
                };

                var validationResponse = await providerService.CreateWalletAsync(providerUserProfile, providerParams, args.env);

                if (validationResponse == null || !validationResponse.IsSuccess)
                    return (false, validationResponse?.Message ?? "Account creation failed");

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
                    TransactionPin = request.TransactionPin,
                    MaxCheckSum= validationResponse.Data?.macChecksum
                };

               var wallet= await _userWalletRepository.CreateUserWallet(walletModel, new UserWalletParams
                {
                    businessCode = businessCode,
                    Env = args.env,
                    UserId = args.UserId
                });

                if (userProfile.Roles.Any(r => r.RoleName == "Organization"))
                {
                    await UpdateOrganizationInputPackage(args, providerId, wallet);
                }

                return (true, "Wallet created successfully");
            
            }
            catch (Exception ex)
            {
                return (false, $"Error creating wallet: {ex.Message}");
            }
        }

        public async Task UpdateOrganizationInputPackage(WalletQueryParams args, long providerId, UserWalletModel wallet)
        {
            var hasOrg = await _businessRepository.GetOrganizationByUserId(args.UserId);
            if (hasOrg != null)
            {
                await _inputFinancingRepository.UpdateInputPackageWallets(
                    hasOrg.OrganizaionId,
                    wallet.WalletId,
                    providerId
                );
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

                var providerParams = await RetrieveParams(request.WalletProviderCode, args.env, args.businessCode);
                var providerId = providerParams.Select(x => x.ProviderId).FirstOrDefault();

                if (wallet.WalletProviderId != providerId)
                    return new ProviderResponse
                    {
                        IsSuccess = false,
                        Message = "Wallet provider mismatch"
                    };

                var providerType = request.WalletProviderCode.ToEnum<WalletProviderType>();
                var providerService = _factory.GetService(request.WalletProviderCode);

                var transactionReference = await _userWalletRepository.CreateWalletFundinAsync(
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

                var providerResponse = await providerService.InitiateWalletTopUpAsync(
                    providerRequest,
                    providerParams,
                    args.env);

                if (providerResponse == null || !providerResponse.IsSuccess)
                {
                    string rf = providerResponse.Data != null ? providerResponse.Data.ProviderReference : null;
                    await _userWalletRepository.CompleteWalletFunding(
                        transactionReference,
                        false,
                        rf,
                        providerResponse?.Message ?? "Provider failed");

                    return new ProviderResponse
                    {
                        IsSuccess = false,
                        Message = providerResponse?.Message ?? "Top-up initiation failed",
                        ProviderReference = rf,
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

        public async Task<bool> GetRequestToPayStatus(string  walletProviderCode,
            string transactionreference, string env,
            string businessCode)

        {
            bool result  = false;
            var providerType = walletProviderCode.ToEnum<WalletProviderType>();

            var providerParams = await RetrieveParams(walletProviderCode,  env, businessCode);
            var providerService = _factory.GetService(walletProviderCode);

            if (providerType == WalletProviderType.Momo)
            {
                var response = await _mtnMomoService.GetRequestToPayStatus(transactionreference, providerParams, env);
                if (response.IsSuccess && response.Data !=null)
                {
                    result = true;
                    var webhookRequest = new ProviderWebhookRequest
                    {
                        Amount=decimal.Parse( response.Data.Amount),
                        ProviderReference=response.Data.FinancialTransactionId,
                        Status= response.Data.Status,
                        TransactionReference=response.Data.ExternalId,
                        RawPayload= JsonSerializer.Serialize(response.Data),
                        BusinessCode=businessCode
                    };

                    await HandleTopUpWebhook(webhookRequest);
                }else
                {
                    result =false;
                }
                 
            }
            return result;
        }
        public async Task<string> HandleTopUpWebhook(ProviderWebhookRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TransactionReference))
                return "Invalid transaction";

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    var transaction =
                        await _userWalletRepository.GetWalletTransactionByRef(
                            request.TransactionReference);

                    if (transaction == null)
                        return "Transaction not found";

                    if (transaction.WalletTransactionStatus ==
                            WalletTransactionStatus.Completed ||
                        transaction.WalletTransactionStatus ==
                            WalletTransactionStatus.Reversed)
                        return "Transaction Completed or Reversed";

                    var isSuccessful = new[]
                    {
                "SUCCESS",
                "SUCCESSFUL"
            }.Contains(request.Status, StringComparer.OrdinalIgnoreCase);

                    var isFailed = request.Status.Equals(
                        "FAILED",
                        StringComparison.OrdinalIgnoreCase);

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

                        return "Transaction Failed";
                    }

                    if (isSuccessful)
                    {
                        var wallet = await _userWalletRepository.GetWalletById(
                            new UserWalletParams
                            {
                                UserId = transaction.UserWallet.UserId,
                                Id = transaction.UserWalletId.Value,
                                businessCode = request.BusinessCode
                            });

                        if (wallet == null)
                            return "Successful";

                        var balanceBefore = wallet.WalletBalance;

                        var netAmount =
                            transaction.TransactionAmount
                            - transaction.TransactionFee
                            - transaction.CommissionFee;

                        wallet.WalletBalance += netAmount;

                        transaction.BalanceBefore = balanceBefore;
                        transaction.BalanceAfter = wallet.WalletBalance;
                        transaction.WalletTransactionStatus =
                            WalletTransactionStatus.Completed;

                        await _userWalletRepository.UpdateWalletBalance(
                            new UserWallet
                            {
                                Id = wallet.Id,
                                WalletBalance = wallet.WalletBalance,
                                IsActive = wallet.IsActive
                            });

                        await _userWalletRepository.UpdateTransaction(transaction);

                        await dbTransaction.CommitAsync();

                        return "Successful";
                    }

                    await _userWalletRepository.UpdateTransactionStatus(
                        transaction.TransactionReference,
                        WalletTransactionStatus.DebitPending);

                    await dbTransaction.CommitAsync();

                    return "Transaction Pending";
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            });
        }
        public async Task<ProviderResponse> WithdrawWallet(WithdrawWalletRequest request, WalletQueryParams args)
        {
            try
            {
                if (request.Amount <= 0)
                    return new ProviderResponse { IsSuccess = false, Message = "Invalid amount" };

                var wallet = await _userWalletRepository.GetWalletById(new UserWalletParams
                {
                    UserId = args.UserId,
                    businessCode = args.businessCode,
                    Env = args.env,
                    Id = request.UserWalletId
                });

                if (wallet == null)
                    return new ProviderResponse { IsSuccess = false, Message = "Wallet not found" };

                if (!wallet.IsActive)
                    return new ProviderResponse { IsSuccess = false, Message = "Wallet is inactive" };

                if (wallet.WalletBalance < request.Amount)
                    return new ProviderResponse { IsSuccess = false, Message = "Insufficient balance" };

                if (string.IsNullOrWhiteSpace(request.TransactionPin))
                    return new ProviderResponse { IsSuccess = false, Message = "Transaction PIN is required" };

                // validate pin via repository (throws on failure)
                try
                {
                    await _userWalletRepository.ValidateTransactionPin(request.UserWalletId, request.TransactionPin);
                }
                catch (Exception ex)
                {
                    return new ProviderResponse { IsSuccess = false, Message = ex.Message };
                }

                var providerParams = await RetrieveParams(request.WalletProviderCode, args.env, args.businessCode);
                var providerId = providerParams.Select(x => x.ProviderId).FirstOrDefault();

                var providerType = request.WalletProviderCode.ToEnum<WalletProviderType>();
                var providerService = _factory.GetService(request.WalletProviderCode);

                // create pending withdrawal transaction
                var transactionReference = await _userWalletRepository
                    .InitiateWalletWithdrawal
                    (request.UserWalletId, request.Amount, request.Description ?? "Withdrawal");

                var disbursementRequest = new TransferRequest
                {
                    AccountNumber = request.RecipientAccount ?? wallet.WalletNumber,
                    Amount = $"{request.Amount}",
                    Currency = wallet.Currency,
                    Message = request.Description ?? "Wallet withdrawal",
                    Reference = transactionReference
                };

                var providerResponse = await providerService.InitiateWalletTransferAsync(
                    disbursementRequest,
                    providerParams, args.env);

                if (providerResponse == null || !providerResponse.IsSuccess)
                {
                    await _userWalletRepository.CompleteWalletWithdrawal(transactionReference, false, providerResponse?.Data?.ToString(), providerResponse?.Message);
                    return new ProviderResponse
                    {
                        IsSuccess = false,
                        Message = providerResponse?.Message ?? "Provider withdrawal failed",
                        ProviderReference = providerResponse?.Data?.ToString()
                    };
                }

                await _userWalletRepository.CompleteWalletWithdrawal(transactionReference, true, providerResponse?.Data?.ToString(), providerResponse?.Message);

                // TODO: send notification to user (SMS/Email)

                return new ProviderResponse
                {
                    IsSuccess = true,
                    Message = providerResponse.Message ?? "Withdrawal initiated",
                    ProviderReference = providerResponse?.Data?.ToString(),
                    Data = new { TransactionReference = transactionReference }
                };
            }
            catch (Exception ex)
            {
                return new ProviderResponse { IsSuccess = false, Message = ex.Message };
            }
        }

    }
}
