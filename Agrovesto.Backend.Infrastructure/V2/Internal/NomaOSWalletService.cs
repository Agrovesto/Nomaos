using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Core.Models.Payment;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        Task<(bool status, string message, CreateWalletResult? data)> CreateWallet(CreateWalletRequest request, WalletQueryParams args);
        Task<bool> GetRequestToPayStatus(string walletProviderCode, string transactionreference, string env,
            string businessCode);
        Task<FarmerFinancialSummaryDto> GetUserFinancialSummary(UserWalletParams args);
        Task<PagedListResponse<List<FarmerTransactionDto>>> GetUserWalletTransaction(ListQueryParams args);
        Task<PagedListResponse<List<UserWalletResponseModel>>> GetUserWallletsByUserId(UserWalletParams args);
        Task<string> HandleTopUpWebhook(ProviderWebhookRequest request);
        Task<ProviderResponse> TopUpWallet(TopUpWalletRequest request, WalletQueryParams args);
        Task UpdateOrganizationInputPackage(WalletQueryParams args, long providerId, UserWalletModel wallet);
        Task<ProviderResponse> WithdrawWallet(WithdrawWalletRequest request, WalletQueryParams args);
        Task<(bool status, string message, WalletTransferReceipt? data)> TransferBetweenWallets(WalletTransferRequest request, WalletQueryParams args);
        Task<(bool status, string message, BankTransferReceipt? data)> TransferToBank(BankTransferRequest request, WalletQueryParams args);
        Task<(bool status, string message, TokenResult? token)> ValidateWalletPin(ValidateWalletPinRequest request, WalletQueryParams args);
        Task<(bool status, string message)> UpdateWalletPin(UpdateWalletPinRequest request, WalletQueryParams args);
        Task<(bool status, string message, WalletPinResetOtpResult? data)> SendWalletPinResetOtp(WalletQueryParams args);
        Task<(bool status, string message, TokenResult? token)> VerifyWalletPinResetOtp(VerifyPinResetOtpRequest request, WalletQueryParams args);
    }

            public class NomaOSWalletService : INomaOSWalletService
    {
        private const int PinResetOtpExpiryMinutes = 5;

        private readonly IWalletServiceFactory _factory;
        private readonly IBusinessRepository _businessRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWalletProviderRepository _providerRepo;
        private readonly IUserWalletRepository _userWalletRepository;
        private readonly DataContext _context;
        private readonly IInputFinancingRepository _inputFinancingRepository;
        private readonly IMTNMomo _mtnMomoService;
        private readonly ILogger<NomaOSWalletService> _logger;
        private readonly IOtpService _otpService;
        private readonly IAWSService _awsService;
        private readonly IConfiguration _configuration;
        public NomaOSWalletService(IBusinessRepository businessRepository,
            IOnboardingRepository onboardingRepository,
            IUserWalletRepository userWalletRepository,
            IWalletProviderRepository providerRepo, IWalletServiceFactory factory,
            DataContext context, IMTNMomo mtnMomoService,
            IInputFinancingRepository inputFinancingRepository,
            ILogger<NomaOSWalletService> logger,
            IOtpService otpService,
            IAWSService awsService,
            IConfiguration configuration)
        {

            _mtnMomoService = mtnMomoService;
            _factory = factory;
            _providerRepo = providerRepo;
            _businessRepository = businessRepository;
            _onboardingRepository = onboardingRepository;
            _context = context;
            _userWalletRepository = userWalletRepository;
            _inputFinancingRepository = inputFinancingRepository;
            _logger = logger;
            _otpService = otpService;
            _awsService = awsService;
            _configuration = configuration;
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

        public async Task<(bool status, string message, CreateWalletResult? data)> CreateWallet(Core.Models.Businesses.CreateWalletRequest request, WalletQueryParams args)
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
                        return (false, $"Business with code {args.businessCode} does not exist", null);

                    businessCode = args.businessCode;
                }

                var userProfile = await _onboardingRepository.GetUseridentity(args.UserId);
                if (userProfile == null)
                    return (false, "User not found", null);

                if(request.BVN.IsNullOrEmpty() is false)
                {
                    if (userProfile.BVN.IsNullOrEmpty())
                    {
                        userProfile.BVN = request.BVN;
                        await _onboardingRepository.UpdateUserDetails(new OnboardingRequestModel() { Bvn = request.BVN}, new OnBoardingArgs() { UserId = args.UserId });
                    }
                }


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
                    return (false, "Wallet already exists for this provider and context", null);

                var providerType = request.WalletProviderCode.ToEnum<WalletProviderType>();
                var providerService = _factory.GetService(request.WalletProviderCode);

                var providerUserProfile = new Agrovesto.Backend.Core.V2.Onboarding.UserProfileResponse
                {
                    UserId = userProfile.UserId,
                    Email = userProfile.Email,
                    PhoneNumber = !string.IsNullOrEmpty( userProfile.Phone) ? userProfile.Phone : request.PhoneNumber,
                    BVN=!string.IsNullOrEmpty(userProfile.BVN) ? userProfile.BVN : request.BVN,
                    NIN= !string.IsNullOrEmpty(userProfile.NIN) ? userProfile.NIN : request.NIN,
                    ImageString= !string.IsNullOrEmpty(userProfile.KYC.Base64) ? userProfile.KYC.Base64 : null,
                    FirstName = userProfile.Name
                };

                var validationResponse = await providerService.CreateWalletAsync(providerUserProfile, providerParams, args.env);

                if (validationResponse == null || !validationResponse.IsSuccess)
                    return (false, validationResponse?.Message ?? "Account creation failed", null);

                var walletNumber = validationResponse.Data?.WalletNumber;
                if (string.IsNullOrWhiteSpace(walletNumber))
                    return (false, "Provider did not return a valid wallet number", null);

                var currency = providerParams.FirstOrDefault(x => x.Key == "Currency")?.Value ?? "NGN";

                var walletModel = new UserWalletModel
                {
                    UserId = args.UserId,
                    WalletProviderId = providerId,
                    WalletNumber = walletNumber,
                    WalletBalance = 0,
                    Currency = currency,
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

                var providerInfo = await _providerRepo.Get(providerId);

                var result = new CreateWalletResult
                {
                    WalletId = wallet.WalletId,
                    UserId = args.UserId,
                    AccountName = validationResponse.Data?.AccountName,
                    AccountNumber = walletNumber,
                    WalletProviderId = providerId,
                    ProviderName = providerInfo?.Name,
                    ProviderCode = request.WalletProviderCode,
                    WalletBalance = walletModel.WalletBalance,
                    Currency = currency,
                    ContextType = contextType.ToString(),
                    ContextReferenceId = contextReferenceId,
                    IsDefault = request.IsDefault
                };

                return (true, "Wallet created successfully", result);

            }
            catch (Exception ex)
            {
                return (false, $"Error creating wallet: {ex.Message}", null);
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

                    decimal? amount = decimal.TryParse(
                        response.Data.Amount,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var parsedAmount)
                        ? parsedAmount
                        : null;

                    var webhookRequest = new ProviderWebhookRequest
                    {
                        Amount = amount,
                        ProviderReference=response.Data.FinancialTransactionId,
                        Status = response.Data.Status ?? string.Empty,
                        TransactionReference = !string.IsNullOrWhiteSpace(response.Data.ExternalId)
                            ? response.Data.ExternalId
                            : transactionreference,
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

                    var isFailed = string.Equals(
                        request.Status,
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

                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();

                        return "Transaction Failed";
                    }

                    if (isSuccessful)
                    {
                        var wallet = transaction.UserWallet;

                        if (wallet == null)
                            return "Wallet not found";

                        var balanceBefore = wallet.WalletBalance;

                        var netAmount =
                            transaction.TransactionAmount
                            - transaction.TransactionFee
                            - transaction.CommissionFee;

                        var balanceAfter = balanceBefore + netAmount;

                        transaction.BalanceBefore = balanceBefore;
                        transaction.BalanceAfter = balanceAfter;
                        transaction.WalletTransactionStatus =
                            WalletTransactionStatus.Completed;
                        transaction.CreditWalletId = transaction.UserWalletId;

                        await _userWalletRepository.UpdateWalletBalance(
                            new UserWallet
                            {
                                Id = wallet.Id,
                                WalletBalance = balanceAfter,
                                IsActive = wallet.IsActive
                            });

                        await _userWalletRepository.UpdateTransaction(transaction);

                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();

                        return "Successful";
                    }

                    await _userWalletRepository.UpdateTransactionStatus(
                        transaction.TransactionReference,
                        WalletTransactionStatus.DebitPending);

                    await _context.SaveChangesAsync();
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

                var user = await _onboardingRepository.GetUserById(args.UserId);
                var senderName = ResolveUserName(user.model);

                if (string.IsNullOrWhiteSpace(senderName))
                {
                    _logger.LogWarning("Wallet withdrawal blocked: no name on file for UserId: {UserId}", args.UserId);
                    return new ProviderResponse
                    {
                        IsSuccess = false,
                        Message = "No account name is on file for this user, which the provider requires to process a withdrawal."
                    };
                }

                var recipientName = !string.IsNullOrWhiteSpace(request.RecipientName)
                    ? request.RecipientName
                    : senderName;

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
                    Reference = transactionReference,
                    SenderName = senderName,
                    SenderAccountNumber = wallet.WalletNumber,
                    RecipientName = recipientName
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

                if (string.Equals(providerResponse.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    await _userWalletRepository.UpdateTransactionProviderInfo(
                        transactionReference, providerResponse.Data?.ProviderReference, "Pending", null);

                    _logger.LogWarning(
                        "Wallet withdrawal {Reference} is pending at the provider; balance left unchanged", transactionReference);

                    return new ProviderResponse
                    {
                        IsSuccess = true,
                        Message = providerResponse.Message ?? "Withdrawal accepted and awaiting confirmation",
                        ProviderReference = providerResponse.Data?.ProviderReference,
                        Data = new { TransactionReference = transactionReference, Status = "Pending" }
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

        public async Task<(bool status, string message, WalletTransferReceipt? data)> TransferBetweenWallets(WalletTransferRequest request, WalletQueryParams args)
        {
            if (request.Amount <= 0)
                return (false, "Transfer amount must be greater than zero", null);

            if (string.IsNullOrWhiteSpace(request.DestinationWalletNumber))
                return (false, "Destination wallet number is required", null);

            if (string.IsNullOrWhiteSpace(request.TransactionPin))
                return (false, "Transaction PIN is required", null);

            var sourceWallet = await _userWalletRepository.GetWalletById(new UserWalletParams
            {
                UserId = args.UserId,
                businessCode = args.businessCode,
                Env = args.env,
                Id = request.SourceWalletId
            });

            if (sourceWallet == null)
                return (false, "Source wallet not found", null);

            if (!sourceWallet.IsActive)
                return (false, "Source wallet is inactive", null);

            var destinationWallet = await _context.UserWallets
                .FirstOrDefaultAsync(x => x.WalletNumber == request.DestinationWalletNumber && !x.IsDeleted);

            if (destinationWallet == null)
                return (false, "Destination wallet not found", null);

            if (!destinationWallet.IsActive)
                return (false, "Destination wallet is inactive", null);

            if (destinationWallet.Id == sourceWallet.Id)
                return (false, "Cannot transfer to the same wallet", null);

            if (sourceWallet.WalletBalance < request.Amount)
                return (false, "Insufficient balance", null);

            try
            {
                await _userWalletRepository.ValidateTransactionPin(sourceWallet.Id, request.TransactionPin);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }

            var sender = await _onboardingRepository.GetUserById(args.UserId);
            var senderName = ResolveUserName(sender.model);

            if (string.IsNullOrWhiteSpace(senderName))
            {
                _logger.LogWarning("Wallet transfer blocked: no name on file for UserId: {UserId}", args.UserId);
                return (false, "No account name is on file for this user, which the provider requires to process a transfer.", null);
            }

            var recipient = await _onboardingRepository.GetUserById(destinationWallet.UserId);
            var recipientName = ResolveUserName(recipient.model);

            if (string.IsNullOrWhiteSpace(recipientName))
                recipientName = destinationWallet.WalletNumber;

            var provider = await _providerRepo.Get(sourceWallet.WalletProviderId);

            if (provider == null)
                return (false, "Wallet provider not found", null);

            var providerParams = await RetrieveParams(provider.Code, args.env, args.businessCode);
            var providerService = _factory.GetService(provider.Code);

            if (providerService == null)
                return (false, $"Wallet service for {provider.Code} was not found", null);

            var isCrossProvider = destinationWallet.WalletProviderId != sourceWallet.WalletProviderId;

            var destinationProvider = provider;
            var destinationParams = providerParams;
            var destinationService = providerService;

            if (isCrossProvider)
            {
                destinationProvider = await _providerRepo.Get(destinationWallet.WalletProviderId);

                if (destinationProvider == null)
                    return (false, "Destination wallet provider not found", null);

                destinationParams = await RetrieveParams(destinationProvider.Code, args.env, args.businessCode);
                destinationService = _factory.GetService(destinationProvider.Code);

                if (destinationService == null)
                    return (false, $"Wallet service for {destinationProvider.Code} was not found", null);
            }

            var description = string.IsNullOrWhiteSpace(request.Description) ? "Wallet transfer" : request.Description;

            var masterReference = string.IsNullOrWhiteSpace(request.ClientReference)
                ? $"{Guid.NewGuid():D}"
                : request.ClientReference.Trim();

            var debitReference = $"{masterReference}-DB";
            var creditReference = $"{masterReference}-CR";

            if (!string.IsNullOrWhiteSpace(request.ClientReference))
            {
                var existing = await _context.UserWalletTransactions
                    .FirstOrDefaultAsync(x => x.TransactionReference == debitReference);

                if (existing != null)
                {
                    _logger.LogInformation(
                        "Wallet transfer {Reference} replayed by UserId: {UserId}; returning the original outcome",
                        masterReference, args.UserId);

                    return (existing.WalletTransactionStatus == WalletTransactionStatus.Completed,
                        "A transfer with this reference has already been submitted",
                        new WalletTransferReceipt
                        {
                            TransactionReference = masterReference,
                            ProviderReference = existing.ProviderReference,
                            Amount = existing.TransactionAmount,
                            Currency = existing.Currency,
                            SourceWalletNumber = sourceWallet.WalletNumber,
                            DestinationWalletNumber = destinationWallet.WalletNumber,
                            SourceBalanceAfter = sourceWallet.WalletBalance,
                            Status = existing.WalletTransactionStatus.ToString()
                        });
                }
            }

            var execution = new TransferExecution
            {
                Args = args,
                Amount = request.Amount,
                Description = description,
                MasterReference = masterReference,
                SourceWallet = sourceWallet,
                DestinationWallet = destinationWallet,
                SourceService = providerService,
                SourceParams = providerParams,
                DestinationService = destinationService,
                DestinationParams = destinationParams,
                SenderName = senderName,
                RecipientName = recipientName
            };

            if (isCrossProvider)
                return await ExecuteCrossProviderTransfer(execution);

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<(bool status, string message, WalletTransferReceipt? data)>(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    await _userWalletRepository.InitiateWalletWithdrawal(
                        sourceWallet.Id, request.Amount, description, debitReference);

                    await _userWalletRepository.CreateWalletFundinAsync(
                        destinationWallet.Id, request.Amount, description, creditReference);

                    WalletResponseService<PaymentResponse>? providerResponse = null;
                    string? failureMessage = null;

                    try
                    {
                        providerResponse = await providerService.InitiateWalletTransferAsync(new TransferRequest
                        {
                            AccountNumber = destinationWallet.WalletNumber,
                            Amount = $"{request.Amount}",
                            Currency = sourceWallet.Currency,
                            Message = description,
                            Reference = masterReference,
                            SenderName = senderName,
                            SenderAccountNumber = sourceWallet.WalletNumber,
                            RecipientName = recipientName
                        }, providerParams, args.env);
                    }
                    catch (Exception ex)
                    {
                        failureMessage = ex.Message;
                        _logger.LogError(ex, "Wallet transfer {Reference} threw before the provider answered", masterReference);
                    }

                    if (failureMessage == null && (providerResponse == null || !providerResponse.IsSuccess))
                        failureMessage = providerResponse?.Message ?? "Wallet service did not return a response";

                    if (failureMessage != null)
                    {
                        await _userWalletRepository.CompleteWalletWithdrawal(debitReference, false, null, failureMessage);
                        await _userWalletRepository.CompleteWalletFunding(creditReference, false, null, failureMessage);
                        await dbTransaction.CommitAsync();

                        _logger.LogWarning("Wallet transfer {Reference} failed: {Message}", masterReference, failureMessage);

                        return (false, failureMessage, null);
                    }

                    var providerReference = providerResponse!.Data?.ProviderReference ?? providerResponse.Data?.Reference;
                    var providerStatus = providerResponse.Data?.Status;

                    if (string.Equals(providerResponse.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                    {
                        await _userWalletRepository.UpdateTransactionProviderInfo(
                            debitReference, providerReference, providerStatus ?? "Pending", null);

                        await _userWalletRepository.UpdateTransactionProviderInfo(
                            creditReference, providerReference, providerStatus ?? "Pending", null);

                        await dbTransaction.CommitAsync();

                        await _userWalletRepository.FlagForReconciliation(
                            debitReference, creditReference,
                            "Provider accepted the transfer but has not confirmed it; a transaction status query is required");

                        _logger.LogWarning(
                            "Wallet transfer {Reference} is pending at the provider; balances left unchanged", masterReference);

                        return (true, "Your transfer has been accepted and is awaiting confirmation.", new WalletTransferReceipt
                        {
                            TransactionReference = masterReference,
                            ProviderReference = providerReference,
                            Amount = request.Amount,
                            Currency = sourceWallet.Currency,
                            SourceWalletNumber = sourceWallet.WalletNumber,
                            DestinationWalletNumber = destinationWallet.WalletNumber,
                            SourceBalanceAfter = sourceWallet.WalletBalance,
                            Status = "Pending"
                        });
                    }

                    await _userWalletRepository.CompleteWalletWithdrawal(debitReference, true, providerReference, providerStatus);

                    try
                    {
                        await _userWalletRepository.CompleteWalletFunding(creditReference, true, providerReference, providerStatus);
                    }
                    catch (Exception ex)
                    {
                        await dbTransaction.CommitAsync();

                        await _userWalletRepository.FlagForReconciliation(
                            debitReference, creditReference, $"Credit leg failed after a successful debit: {ex.Message}");

                        _logger.LogError(ex,
                            "Wallet transfer {Reference} debited but could not credit the destination", masterReference);

                        return (false,
                            "Your transfer was debited but the destination could not be credited. Our team has been notified to reconcile it.",
                            null);
                    }

                    await dbTransaction.CommitAsync();

                    var balanceAfter = await _context.UserWallets
                        .Where(x => x.Id == sourceWallet.Id)
                        .Select(x => x.WalletBalance)
                        .FirstOrDefaultAsync();

                    _logger.LogInformation("Wallet transfer {Reference} completed for UserId: {UserId}", masterReference, args.UserId);

                    return (true, "Transfer completed successfully", new WalletTransferReceipt
                    {
                        TransactionReference = masterReference,
                        ProviderReference = providerReference,
                        Amount = request.Amount,
                        Currency = sourceWallet.Currency,
                        SourceWalletNumber = sourceWallet.WalletNumber,
                        DestinationWalletNumber = destinationWallet.WalletNumber,
                        SourceBalanceAfter = balanceAfter,
                        Status = providerStatus ?? "Completed"
                    });
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<(bool status, string message, BankTransferReceipt? data)> TransferToBank(BankTransferRequest request, WalletQueryParams args)
        {
            if (request.Amount <= 0)
                return (false, "Transfer amount must be greater than zero", null);

            if (string.IsNullOrWhiteSpace(request.DestinationAccountNumber))
                return (false, "Destination account number is required", null);

            if (string.IsNullOrWhiteSpace(request.BankCode))
                return (false, "Bank code is required", null);

            if (string.IsNullOrWhiteSpace(request.DestinationAccountName))
                return (false, "Destination account name is required. Resolve it with a name enquiry before transferring.", null);

            if (string.IsNullOrWhiteSpace(request.TransactionPin))
                return (false, "Transaction PIN is required", null);

            var destinationAccountNumber = request.DestinationAccountNumber.Trim();
            var bankCode = request.BankCode.Trim();
            var destinationAccountName = request.DestinationAccountName.Trim();

            var sourceWallet = await _userWalletRepository.GetWalletById(new UserWalletParams
            {
                UserId = args.UserId,
                businessCode = args.businessCode,
                Env = args.env,
                Id = request.SourceWalletId
            });

            if (sourceWallet == null)
                return (false, "Source wallet not found", null);

            if (!sourceWallet.IsActive)
                return (false, "Source wallet is inactive", null);

            if (string.Equals(sourceWallet.WalletNumber, destinationAccountNumber, StringComparison.Ordinal))
                return (false, "Cannot transfer to the same account", null);

            var destinationIsInternalWallet = await _context.UserWallets
                .AnyAsync(x => x.WalletNumber == destinationAccountNumber && !x.IsDeleted);

            if (destinationIsInternalWallet)
                return (false, "That account number belongs to a wallet on this platform. Use the wallet transfer endpoint so both ledgers stay in step.", null);

            if (sourceWallet.WalletBalance < request.Amount)
                return (false, "Insufficient balance", null);

            try
            {
                await _userWalletRepository.ValidateTransactionPin(sourceWallet.Id, request.TransactionPin);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }

            var sender = await _onboardingRepository.GetUserById(args.UserId);
            var senderName = ResolveUserName(sender.model);

            if (string.IsNullOrWhiteSpace(senderName))
            {
                _logger.LogWarning("Bank transfer blocked: no name on file for UserId: {UserId}", args.UserId);
                return (false, "No account name is on file for this user, which the provider requires to process a transfer.", null);
            }

            var provider = await _providerRepo.Get(sourceWallet.WalletProviderId);

            if (provider == null)
                return (false, "Wallet provider not found", null);

            var providerService = _factory.GetService(provider.Code);

            if (providerService == null)
                return (false, $"Wallet service for {provider.Code} was not found", null);

            if (!providerService.SupportsBankTransfer)
                return (false, $"{provider.Code} wallets cannot pay out to a bank account", null);

            var providerParams = await RetrieveParams(provider.Code, args.env, args.businessCode);

            var description = string.IsNullOrWhiteSpace(request.Description) ? "Bank transfer" : request.Description;

            var masterReference = string.IsNullOrWhiteSpace(request.ClientReference)
                ? $"{Guid.NewGuid():D}"
                : request.ClientReference.Trim();

            var debitReference = $"{masterReference}-DB";

            if (!string.IsNullOrWhiteSpace(request.ClientReference))
            {
                var existing = await _context.UserWalletTransactions
                    .FirstOrDefaultAsync(x => x.TransactionReference == debitReference);

                if (existing != null)
                {
                    _logger.LogInformation(
                        "Bank transfer {Reference} replayed by UserId: {UserId}; returning the original outcome",
                        masterReference, args.UserId);

                    return (existing.WalletTransactionStatus == WalletTransactionStatus.Completed,
                        "A transfer with this reference has already been submitted",
                        new BankTransferReceipt
                        {
                            TransactionReference = masterReference,
                            ProviderReference = existing.ProviderReference,
                            Amount = existing.TransactionAmount,
                            Currency = existing.Currency,
                            SourceWalletNumber = sourceWallet.WalletNumber,
                            DestinationAccountNumber = destinationAccountNumber,
                            BankCode = bankCode,
                            DestinationAccountName = destinationAccountName,
                            SourceBalanceAfter = sourceWallet.WalletBalance,
                            Status = existing.WalletTransactionStatus.ToString()
                        });
                }
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<(bool status, string message, BankTransferReceipt? data)>(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    await _userWalletRepository.InitiateWalletWithdrawal(
                        sourceWallet.Id, request.Amount, description, debitReference);

                    WalletResponseService<PaymentResponse>? providerResponse = null;
                    string? failureMessage = null;

                    try
                    {
                        providerResponse = await providerService.InitiateWalletTransferAsync(new TransferRequest
                        {
                            AccountNumber = destinationAccountNumber,
                            BankCode = bankCode,
                            Amount = $"{request.Amount}",
                            Currency = sourceWallet.Currency,
                            Message = description,
                            Reference = masterReference,
                            SenderName = senderName,
                            SenderAccountNumber = sourceWallet.WalletNumber,
                            RecipientName = destinationAccountName
                        }, providerParams, args.env);
                    }
                    catch (Exception ex)
                    {
                        failureMessage = ex.Message;
                        _logger.LogError(ex, "Bank transfer {Reference} threw before the provider answered", masterReference);
                    }

                    if (failureMessage == null && (providerResponse == null || !providerResponse.IsSuccess))
                        failureMessage = providerResponse?.Message ?? "Wallet service did not return a response";

                    if (failureMessage != null)
                    {
                        await _userWalletRepository.CompleteWalletWithdrawal(debitReference, false, null, failureMessage);
                        await dbTransaction.CommitAsync();

                        _logger.LogWarning("Bank transfer {Reference} failed: {Message}", masterReference, failureMessage);

                        return (false, failureMessage, null);
                    }

                    var providerReference = providerResponse!.Data?.ProviderReference ?? providerResponse.Data?.Reference;
                    var providerStatus = providerResponse.Data?.Status;

                    if (string.Equals(providerResponse.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                    {
                        await _userWalletRepository.UpdateTransactionProviderInfo(
                            debitReference, providerReference, providerStatus ?? "Pending", null);

                        await dbTransaction.CommitAsync();

                        await _userWalletRepository.FlagForReconciliation(
                            debitReference, debitReference,
                            "Provider accepted the bank transfer but has not confirmed it; a transaction status query is required");

                        _logger.LogWarning(
                            "Bank transfer {Reference} is pending at the provider; balance left unchanged", masterReference);

                        return (true, "Your transfer has been accepted and is awaiting confirmation.", new BankTransferReceipt
                        {
                            TransactionReference = masterReference,
                            ProviderReference = providerReference,
                            Amount = request.Amount,
                            Currency = sourceWallet.Currency,
                            SourceWalletNumber = sourceWallet.WalletNumber,
                            DestinationAccountNumber = destinationAccountNumber,
                            BankCode = bankCode,
                            DestinationAccountName = destinationAccountName,
                            SourceBalanceAfter = sourceWallet.WalletBalance,
                            Status = "Pending"
                        });
                    }

                    await _userWalletRepository.CompleteWalletWithdrawal(debitReference, true, providerReference, providerStatus);

                    await dbTransaction.CommitAsync();

                    var balanceAfter = await _context.UserWallets
                        .Where(x => x.Id == sourceWallet.Id)
                        .Select(x => x.WalletBalance)
                        .FirstOrDefaultAsync();

                    _logger.LogInformation("Bank transfer {Reference} completed for UserId: {UserId}", masterReference, args.UserId);

                    return (true, "Transfer completed successfully", new BankTransferReceipt
                    {
                        TransactionReference = masterReference,
                        ProviderReference = providerReference,
                        Amount = request.Amount,
                        Currency = sourceWallet.Currency,
                        SourceWalletNumber = sourceWallet.WalletNumber,
                        DestinationAccountNumber = destinationAccountNumber,
                        BankCode = bankCode,
                        DestinationAccountName = destinationAccountName,
                        SourceBalanceAfter = balanceAfter,
                        Status = providerStatus ?? "Completed"
                    });
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<(bool status, string message, TokenResult? token)> ValidateWalletPin(ValidateWalletPinRequest request, WalletQueryParams args)
        {
            if (string.IsNullOrWhiteSpace(request.Pin))
            {
                _logger.LogWarning("Transaction PIN is required for UserId: {UserId}, WalletId: {WalletId}", args.UserId, request.WalletId);
                throw new BaseException(ExceptionType.BAD_REQUEST, "Transaction PIN is required");
            }

            var wallet = await _userWalletRepository.GetWalletById(new UserWalletParams
            {
                UserId = args.UserId,
                businessCode = args.businessCode,
                Env = args.env,
                Id = request.WalletId
            });
            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for UserId: {UserId}, WalletId: {WalletId}", args.UserId, request.WalletId);
                throw new BaseException(ExceptionType.NOTFOUND, "Wallet not found");
            }

            try
            {
                await _userWalletRepository.ValidateTransactionPin(wallet.Id, request.Pin);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Invalid transaction PIN for UserId: {UserId}, WalletId: {WalletId}", args.UserId, request.WalletId);
                return (false, ex.Message, null);
            }

            var token = await _otpService.GenerateTokenAsync(args.UserId, OtpPurpose.WalletPinUpdate);

            return (true, "PIN is valid", token);
        }

        public async Task<(bool status, string message)> UpdateWalletPin(UpdateWalletPinRequest request, WalletQueryParams args)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                _logger.LogWarning("Token is required for UserId: {UserId}, WalletId: {WalletId}", args.UserId, request.WalletId);
                throw new BaseException(ExceptionType.BAD_REQUEST, "Token is required");
            }

            var (isTokenValid, tokenMessage) = await _otpService.ValidateTokenAsync(args.UserId, request.Token, OtpPurpose.WalletPinUpdate);
            if (!isTokenValid)
            {
                _logger.LogWarning("Wallet pin update rejected for UserId: {UserId}, WalletId: {WalletId} - {Reason}", args.UserId, request.WalletId, tokenMessage);
                return (false, tokenMessage);
            }

            if (string.IsNullOrWhiteSpace(request.NewPin) || string.IsNullOrWhiteSpace(request.ConfirmNewPin))
                throw new BaseException(ExceptionType.BAD_REQUEST, "New PIN and confirmation PIN are required");

            if (request.NewPin != request.ConfirmNewPin)
                return (false, "New PIN and confirmation PIN do not match");

            var wallet = await _userWalletRepository.GetWalletById(new UserWalletParams
            {
                UserId = args.UserId,
                businessCode = args.businessCode,
                Env = args.env,
                Id = request.WalletId
            });
            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for UserId: {UserId}, WalletId: {WalletId}", args.UserId, request.WalletId);
                throw new BaseException(ExceptionType.NOTFOUND, "Wallet not found");
            }

            await _userWalletRepository.UpdateTransactionPin(wallet.Id, request.NewPin);
            await _otpService.InvalidateTokenAsync(args.UserId, request.Token, OtpPurpose.WalletPinUpdate);

            return (true, "Wallet PIN updated successfully");
        }

        public async Task<(bool status, string message, WalletPinResetOtpResult? data)> SendWalletPinResetOtp(WalletQueryParams args)
        {
            if (string.IsNullOrWhiteSpace(args.UserId))
                throw new BaseException(ExceptionType.BAD_REQUEST, "UserId is required");

            var user = await _onboardingRepository.GetUserById(args.UserId);
            if (!user.status || user.model == null)
            {
                _logger.LogWarning("Wallet PIN reset OTP requested for unknown UserId: {UserId}", args.UserId);
                throw new BaseException(ExceptionType.NOTFOUND, "User not found");
            }

            var email = user.model.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Wallet PIN reset OTP requested but no email is on file for UserId: {UserId}", args.UserId);
                return (false, "No email address is on file for this account", null);
            }

            // Throws BAD_REQUEST if the user has burnt their 3-per-10-minutes OTP allowance.
            var otp = await _otpService.GenerateOtpAsync(new GenerateOtpRequest
            {
                UserId = args.UserId,
                Target = OtpTarget.Email,
                Destination = email,
                Purpose = OtpPurpose.WalletPinReset,
                ExpiryMinutes = PinResetOtpExpiryMinutes
            });

            var body = $"Your wallet PIN reset code is {otp.Code}. It expires in {PinResetOtpExpiryMinutes} minutes. " +
                       "If you did not request this, ignore this email and your PIN stays unchanged.";

            var delivered = await _awsService.SendEmailAsync(
                _configuration["Official:Email"],
                email,
                "Reset your wallet PIN",
                body,
                body);

            if (!delivered)
            {
                await _otpService.InvalidateActiveOtps(args.UserId, OtpTarget.Email);

                _logger.LogError(
                    "Wallet PIN reset OTP could not be emailed for UserId: {UserId}. See the preceding SES error.",
                    args.UserId);

                return (false, "We could not send the reset code right now. Please try again shortly.", null);
            }

            _logger.LogInformation("Wallet PIN reset OTP sent for UserId: {UserId}", args.UserId);

            return (true, "A reset code has been sent to your email address", new WalletPinResetOtpResult
            {
                Destination = MaskEmail(email),
                ExpiresAt = new DateTimeOffset(otp.ExpiresAt, TimeSpan.Zero)
            });
        }

        public async Task<(bool status, string message, TokenResult? token)> VerifyWalletPinResetOtp(VerifyPinResetOtpRequest request, WalletQueryParams args)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Reset code is required");

            // Consumes the code on success, and counts the attempt on failure.
            var otp = await _otpService.VerifyOtpAsync(args.UserId, request.Code, OtpPurpose.WalletPinReset);
            if (otp == null)
            {
                _logger.LogWarning("Wallet PIN reset code rejected for UserId: {UserId}", args.UserId);
                return (false, "The reset code is invalid or has expired", null);
            }

            // The code is spent, so hand back the same token that a successful PIN
            // check issues - update-pin accepts either without knowing which it got.
            var token = await _otpService.GenerateTokenAsync(args.UserId, OtpPurpose.WalletPinUpdate);

            _logger.LogInformation("Wallet PIN reset code accepted for UserId: {UserId}", args.UserId);

            return (true, "Reset code is valid", token);
        }

        private sealed class TransferExecution
        {
            public WalletQueryParams Args { get; init; } = null!;
            public decimal Amount { get; init; }
            public string Description { get; init; } = null!;
            public string MasterReference { get; init; } = null!;
            public UserWalletDetailResponseModel SourceWallet { get; init; } = null!;
            public UserWallet DestinationWallet { get; init; } = null!;
            public IGenericWalletService SourceService { get; init; } = null!;
            public WalletProviderParameterModel[] SourceParams { get; init; } = null!;
            public IGenericWalletService DestinationService { get; init; } = null!;
            public WalletProviderParameterModel[] DestinationParams { get; init; } = null!;
            public string SenderName { get; init; } = null!;
            public string RecipientName { get; init; } = null!;

            public string DebitReference => $"{MasterReference}-DB";
            public string CreditReference => $"{MasterReference}-CR";
            public string ReversalReference => $"{MasterReference}-RV";
        }

        private WalletTransferReceipt BuildReceipt(TransferExecution execution, string? providerReference, decimal sourceBalanceAfter, string status) =>
            new()
            {
                TransactionReference = execution.MasterReference,
                ProviderReference = providerReference,
                Amount = execution.Amount,
                Currency = execution.SourceWallet.Currency,
                SourceWalletNumber = execution.SourceWallet.WalletNumber,
                DestinationWalletNumber = execution.DestinationWallet.WalletNumber,
                SourceBalanceAfter = sourceBalanceAfter,
                Status = status
            };

        private async Task<(bool status, string message, WalletTransferReceipt? data)> ExecuteCrossProviderTransfer(TransferExecution execution)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<(bool status, string message, WalletTransferReceipt? data)>(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    await _userWalletRepository.InitiateWalletWithdrawal(
                        execution.SourceWallet.Id, execution.Amount, execution.Description, execution.DebitReference);

                    await _userWalletRepository.CreateWalletFundinAsync(
                        execution.DestinationWallet.Id, execution.Amount, execution.Description, execution.CreditReference);

                    var debit = await CallProvider(() => execution.SourceService.DebitWalletAsync(
                        new WalletTopUpProviderRequest
                        {
                            WalletNumber = execution.SourceWallet.WalletNumber,
                            WalletName = execution.SenderName,
                            Amount = execution.Amount,
                            Currency = execution.SourceWallet.Currency,
                            Message = execution.Description,
                            TransactionReference = execution.DebitReference
                        },
                        execution.SourceParams,
                        execution.Args.env));

                    if (!debit.ok)
                    {
                        await _userWalletRepository.CompleteWalletWithdrawal(execution.DebitReference, false, null, debit.message);
                        await _userWalletRepository.CompleteWalletFunding(execution.CreditReference, false, null, debit.message);
                        await dbTransaction.CommitAsync();

                        _logger.LogWarning(
                            "Cross-provider transfer {Reference} failed on the debit leg: {Message}",
                            execution.MasterReference, debit.message);

                        return (false, debit.message, null);
                    }

                    if (debit.pending)
                    {
                        await _userWalletRepository.UpdateTransactionProviderInfo(
                            execution.DebitReference, debit.providerReference, "Pending", null);
                        await _userWalletRepository.UpdateTransactionProviderInfo(
                            execution.CreditReference, debit.providerReference, "Pending", null);

                        await dbTransaction.CommitAsync();

                        await _userWalletRepository.FlagForReconciliation(
                            execution.DebitReference, execution.CreditReference,
                            "Debit leg is pending at the source provider; a transaction status query is required before the destination is credited");

                        _logger.LogWarning(
                            "Cross-provider transfer {Reference} is pending on the debit leg; balances left unchanged",
                            execution.MasterReference);

                        return (true, "Your transfer has been accepted and is awaiting confirmation.",
                            BuildReceipt(execution, debit.providerReference, execution.SourceWallet.WalletBalance, "Pending"));
                    }

                    await _userWalletRepository.CompleteWalletWithdrawal(
                        execution.DebitReference, true, debit.providerReference, debit.status);

                    var credit = await CallProvider(() => execution.DestinationService.InitiateWalletTransferAsync(
                        new TransferRequest
                        {
                            AccountNumber = execution.DestinationWallet.WalletNumber,
                            Amount = $"{execution.Amount}",
                            Currency = execution.DestinationWallet.Currency,
                            Message = execution.Description,
                            Reference = execution.CreditReference,
                            SenderName = execution.SenderName,
                            SenderAccountNumber = execution.SourceWallet.WalletNumber,
                            RecipientName = execution.RecipientName
                        },
                        execution.DestinationParams,
                        execution.Args.env));

                    if (!credit.ok)
                        return await ReverseCrossProviderDebit(execution, dbTransaction, credit.message);

                    if (credit.pending)
                    {
                        await _userWalletRepository.UpdateTransactionProviderInfo(
                            execution.CreditReference, credit.providerReference, "Pending", null);

                        await dbTransaction.CommitAsync();

                        await _userWalletRepository.FlagForReconciliation(
                            execution.DebitReference, execution.CreditReference,
                            "Destination provider accepted the credit but has not confirmed it; a transaction status query is required");

                        var debitedBalance = await ReadWalletBalance(execution.SourceWallet.Id);

                        _logger.LogWarning(
                            "Cross-provider transfer {Reference} debited; credit leg pending at the destination provider",
                            execution.MasterReference);

                        return (true, "Your transfer has been sent and is awaiting confirmation from the receiving provider.",
                            BuildReceipt(execution, credit.providerReference, debitedBalance, "Pending"));
                    }

                    await _userWalletRepository.CompleteWalletFunding(
                        execution.CreditReference, true, credit.providerReference, credit.status);

                    await dbTransaction.CommitAsync();

                    var balanceAfter = await ReadWalletBalance(execution.SourceWallet.Id);

                    _logger.LogInformation(
                        "Cross-provider transfer {Reference} completed for UserId: {UserId}",
                        execution.MasterReference, execution.Args.UserId);

                    return (true, "Transfer completed successfully",
                        BuildReceipt(execution, credit.providerReference, balanceAfter, "Successful"));
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            });
        }

        private async Task<(bool status, string message, WalletTransferReceipt? data)> ReverseCrossProviderDebit(
            TransferExecution execution,
            IDbContextTransaction dbTransaction,
            string creditFailureMessage)
        {
            await _userWalletRepository.CompleteWalletFunding(
                execution.CreditReference, false, null, creditFailureMessage);

            var reversal = await CallProvider(() => execution.SourceService.InitiateWalletTransferAsync(
                new TransferRequest
                {
                    AccountNumber = execution.SourceWallet.WalletNumber,
                    Amount = $"{execution.Amount}",
                    Currency = execution.SourceWallet.Currency,
                    Message = $"Reversal of {execution.MasterReference}",
                    Reference = execution.ReversalReference,
                    SenderName = execution.SenderName,
                    SenderAccountNumber = execution.SourceWallet.WalletNumber,
                    RecipientName = execution.SenderName
                },
                execution.SourceParams,
                execution.Args.env));

            if (!reversal.ok || reversal.pending)
            {
                await dbTransaction.CommitAsync();

                await _userWalletRepository.FlagForReconciliation(
                    execution.DebitReference, execution.CreditReference,
                    $"Credit leg failed ({creditFailureMessage}) and the reversal did not confirm ({reversal.message})");

                _logger.LogError(
                    "Cross-provider transfer {Reference} debited but neither credited nor reversed: credit={CreditMessage}, reversal={ReversalMessage}",
                    execution.MasterReference, creditFailureMessage, reversal.message);

                return (false,
                    "Your transfer could not be completed and the debit could not be automatically reversed. Our team has been notified to resolve it.",
                    null);
            }

            await _userWalletRepository.CreateWalletFundinAsync(
                execution.SourceWallet.Id, execution.Amount,
                $"Reversal of {execution.MasterReference}", execution.ReversalReference);

            await _userWalletRepository.CompleteWalletFunding(
                execution.ReversalReference, true, reversal.providerReference, "Reversed");

            await dbTransaction.CommitAsync();

            var balanceAfter = await ReadWalletBalance(execution.SourceWallet.Id);

            _logger.LogWarning(
                "Cross-provider transfer {Reference} failed on the credit leg and was reversed: {Message}",
                execution.MasterReference, creditFailureMessage);

            return (false,
                $"Your transfer could not be completed and has been reversed. {creditFailureMessage}",
                BuildReceipt(execution, reversal.providerReference, balanceAfter, "Reversed"));
        }

        private async Task<(bool ok, bool pending, string? providerReference, string? status, string message)> CallProvider(
            Func<Task<WalletResponseService<PaymentResponse>>> call)
        {
            try
            {
                var response = await call();

                if (response == null)
                    return (false, false, null, null, "Wallet service did not return a response");

                var providerReference = response.Data?.ProviderReference ?? response.Data?.Reference;

                if (!response.IsSuccess)
                    return (false, false, providerReference, response.Data?.Status, response.Message ?? "Wallet service rejected the request");

                var pending = string.Equals(response.Status, "Pending", StringComparison.OrdinalIgnoreCase);

                return (true, pending, providerReference, response.Data?.Status, response.Message ?? string.Empty);
            }
            catch (Exception ex)
            {
                return (false, false, null, null, ex.Message);
            }
        }

        private async Task<decimal> ReadWalletBalance(long walletId) =>
            await _context.UserWallets
                .Where(x => x.Id == walletId)
                .Select(x => x.WalletBalance)
                .FirstOrDefaultAsync();

        private static string ResolveUserName(UserModel? user)
        {
            if (user == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(user.Name))
                return user.Name.Trim();

            return $"{user.FirstName} {user.LastName}".Trim();
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');

            return at <= 1
                ? email
                : $"{email[0]}{new string('*', at - 1)}{email.Substring(at)}";
        }
    }
}
