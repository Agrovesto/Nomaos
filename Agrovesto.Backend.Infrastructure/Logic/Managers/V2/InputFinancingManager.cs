using AfricasTalkingCS;
using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Core.V2.Events;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IInputFinancingManager
    {
        Task<bool> AddSubscriberAsync(AddSubscriberRequest request, InputSubscriptionRequestArgs query);
        Task<bool> ApproveSubscriptionAsync(ApproveSubscriptionRequest request, InputSubscriptionRequestArgs query);
        Task<EopResponseDto> CreateEopAsync(CreateEopRequest request, ListQueryParams query);
        Task<bool> CreateInputFinancingPackageConfig(CreateInputPackageRequest request, ListQueryParams query);
        Task<bool> DeactivateInputPackage(ReplacePackageItemRequestArgs query);
        Task<byte[]> ExportSubscriptionsAsync(SubscriptionQueryFilter filter, ExportFormat format);
      //  Task<bool> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request, ListQueryParams query);
        Task<EopDto> GetEopByIdAsync(ListQueryParams query);
        Task<PagedResult<EopDto>> GetEopsAsync(EopFilterRequest request, ListQueryParams query);
        Task<PagedResult<InputObligationDto>> GetFarmerInputObligations(SubscriptionQueryFilter filter, int pageNumber, int pageSize);
        Task<PagedResult<FinancingBreakdownDto>> GetFinancingBreakdownAsync(ListQueryParams args);
        Task<FinancingRepaymentSummaryDto> GetFinancingRepaymentSummaryAsync(ListQueryParams args);
        Task<InputFinancingDashboardDto> GetInputFinancingDashboardAsync(SubscriptionQueryFilter filter);
        Task<InputLoanSummaryDto> GetInputLoanSummaryAsync(ListQueryParams args);
        Task<LoanDetailDto> GetLoanDetailAsync(ListQueryParams args);
        Task<InputPackageDto> GetPackageByIdAsync(ListQueryParams query);
        Task<PagedResult<InputPackageListModel>> GetPackagesAsync(ListQueryParams query);
        Task<SubscriptionFullDetailDto> FetchSubscriptionForReview(SubscriptionQueryFilter filter);
        Task<PagedResult<RepaymentBreakdownDto>> GetRepaymentBreakdownAsync(long subscriptionId, ListQueryParams args);
        Task<PagedResult<SubscriptionDetailDto>> GetSubscriptionsAsync(SubscriptionQueryFilter filter, int pageNumber, int pageSize);
        Task<bool> ReactivatePackageAsync(ReplacePackageItemRequestArgs query);
        Task<bool> RemoveItemFromPackage(ReplacePackageItemRequestArgs query);
        Task<bool> ReplacePackageItemAsync(ReplacePackageItemRequest request, ReplacePackageItemRequestArgs query);
        Task<bool> UpdateInputPackageConfig(long packageId, UpdateInputPackageConfigRequest request, InputPackageConfigArgs args);
        Task<(byte[] FileBytes, string FileName)> DownloadCommissionLetterAsync(long subscriptionId);
        Task SendCommitmentReminderAsync(SubscriptionQueryFilter filter);
        Task<SubscriptionReviewResponseDto> GetSubscriptionDetail(SubscriptionQueryFilter filter);
        Task<bool> AddItemToPackageAsync(AddPackageItemRequest request, ReplacePackageItemRequestArgs query);
        Task<bool> AddAdditionalCostToPackageAsync(AddAdditionalCostRequest request, ReplacePackageItemRequestArgs query);
        Task<bool> RemoveAdditionalCostFromPackageAsync(ReplacePackageItemRequestArgs query);
        Task<bool> UpdateAdditionalCostAsync(UpdateAdditionalCostRequest request, UpdateAdditionalCostArgs query);
        Task<bool> CommitmentPaymentAsync(RecordCommitmentPaymentRequest request, InputSubscriptionRequestArgs args);
    }

    public class InputFinancingManager : IInputFinancingManager
    {
        private readonly IUserWalletRepository _userWalletRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IInputFinancingRepository _inputFinancingRepository;
        private readonly IWalletProviderRepository _walletproviderRepo;
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly IAWSService _awsService;
        private readonly INotificationService _sMSService;
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        private readonly IWalletServiceFactory _factory;
        private readonly IWalletProviderRepository _providerRepo;
        private readonly IMTNMomo _mtnMomoService;
        private readonly IUserWalletRepository _walletTransactionRepository;

        public InputFinancingManager(IWalletServiceFactory factory, IWalletProviderRepository providerRepo, IUserWalletRepository userWalletRepository,
            IInputFinancingRepository inputFinancingRepository, IBusinessRepository businessRepository,
            IWalletProviderRepository walletProviderRepository, IOnBoardingManager onBoardingManager, IAWSService aWSService,
            IConfiguration configuration, INotificationService notificationService,
            DataContext context, IMTNMomo mtnMomoService,
            IUserWalletRepository walletTransactionRepository
)
        {
            _walletTransactionRepository = walletTransactionRepository;
            _userWalletRepository = userWalletRepository;
            _providerRepo = providerRepo;
            _userWalletRepository = userWalletRepository;
            _inputFinancingRepository = inputFinancingRepository;
            _businessRepository = businessRepository;
            _walletproviderRepo = walletProviderRepository;
            _onBoardingManager = onBoardingManager;
            _awsService = aWSService;
            _configuration = configuration;
            _sMSService = notificationService;
            _context= context;
            _mtnMomoService = mtnMomoService;
            _factory= factory;
        }
        private async Task FetchOrganizationDetails(ListQueryParams query)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                query.Id = hasOrg.OrganizaionId;
            }
        }

        public async Task<EopResponseDto> CreateEopAsync(CreateEopRequest request, ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                request.OrganizationId = hasOrg.OrganizaionId;
            }

            return await _inputFinancingRepository.CreateEopAsync(request);
        }

        public async Task<bool> CreateInputFinancingPackageConfig(CreateInputPackageRequest request, ListQueryParams query)
        {
            if (request == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Request cannot be null");

            if (query == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Query cannot be null");

            var args = new InputPackageConfigArgs
            {
                createdByUserId = query.UserId
            };

            var organization = await _businessRepository
          .GetOrganizationByCodeAsync(query.organizationCode);

            if (organization == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Organization not found");

            args.organizationId = organization.OrganizaionId;

            if (request.PaybackWalletId <= 0)
            {
                return await _inputFinancingRepository.CreateInputFinancingPackageConfig(request, args);
            }

            var provider = await _walletproviderRepo.Get(request.PaybackWalletId);

            if (provider == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Wallet Provider not found");

            var userWallet = await _userWalletRepository.GetUserWalletByProviderId(new UserWalletParams
            {
                Id = request.PaybackWalletId,
                UserId = query.UserId,
                businessCode = query.businessCode
            });

            if (userWallet != null)
            {
                args.isSelectedWalletProvisioned = true;
                request.UserWalletId = userWallet.Id;
            }
            else
            {
                args.isSelectedWalletProvisioned = false;
            }

         
            return await _inputFinancingRepository.CreateInputFinancingPackageConfig(request, args);
        }

        //public async Task<bool> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request, ListQueryParams query)
        //{
        //    var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
        //    if (hasOrg != null)
        //    {
        //        request.OrganizationId = hasOrg.OrganizaionId;
        //    }

        //    return await _inputFinancingRepository.GenerateInputPackageFromEopAsync(request);
        //}

        public async Task<EopDto> GetEopByIdAsync(ListQueryParams query)
        {
            await FetchOrganizationDetails(query);
            return await _inputFinancingRepository.GetEopByIdAsync(query.Id);
        }

        public async Task<PagedResult<EopDto>> GetEopsAsync(EopFilterRequest request, ListQueryParams query)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                request.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.GetEopsAsync(request);
        }

        public async Task<InputPackageDto> GetPackageByIdAsync(ListQueryParams query)
        {
            var package = await _inputFinancingRepository.GetPackageByIdAsync(query.Id);

            if (package == null)
                throw new Exception($"Package not found: {query.Id}");

            if (package.IsRepaymentWalletConfigured)
            {
                if (package.WalletProviderId == null)
                    throw new Exception("Wallet provider not configured for this package.");

                var userWallet = await _userWalletRepository.GetUserWalletByProviderId(new UserWalletParams
                {
                    Id = package.WalletProviderId,
                    UserId = query.UserId,
                    businessCode = query.businessCode
                });

                if (userWallet == null)
                    throw new Exception("User wallet not found for repayment configuration.");

                package.RepaymentWallet = new InputPackageRepaymentWallet
                {
                    WalletId = userWallet.Id,
                    WalletName = userWallet.WalletNumber,
                    WalletNumber = userWallet.WalletNumber,
                    WalletProvider = userWallet.ProviderName
                };
            }

            return package;
        }
        public async Task<bool> DeactivateInputPackage(ReplacePackageItemRequestArgs query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.OrganizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.DeactivatePackageAsync(query.PackageId, query.OrganizationId);
        }
        public async Task<bool> UpdateInputPackageConfig(
        long packageId,
        UpdateInputPackageConfigRequest request,
        InputPackageConfigArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (packageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(packageId));
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(args.OrganizationCode);
            if (hasOrg != null)
            {
                args.organizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.UpdateInputPackageConfig(packageId, request, args);
        }

        public async Task<bool> ReactivatePackageAsync(ReplacePackageItemRequestArgs query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.OrganizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.ReactivatePackageAsync(query.PackageId, query.OrganizationId);
        }
        public async Task<PagedResult<InputPackageListModel>> GetPackagesAsync(ListQueryParams query)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.GetPackagesAsync(query);
        }

        public async Task<bool> RemoveItemFromPackage(ReplacePackageItemRequestArgs query)
        {
            if(query == null) throw new ArgumentNullException(nameof(query));
            if(query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            if(query.InputId <= 0) throw new ArgumentException("Invalid InputId", nameof(query.InputId));
            return await _inputFinancingRepository.RemoveItemFromPackage(query.PackageId, query.InputId);
        }
       
        public async Task<bool> ReplacePackageItemAsync(ReplacePackageItemRequest request, ReplacePackageItemRequestArgs query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            if (query.InputId <= 0) throw new ArgumentException("Invalid InputId", nameof(query.InputId));
           if (request == null) throw new ArgumentNullException(nameof(request));
           if(request.OrganizationProductId <= 0) throw new ArgumentException("Invalid OrganizationProductId", nameof(request.OrganizationProductId));
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.OrganizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.ReplacePackageItemAsync(request, query);
        }
        public async Task<bool> AddItemToPackageAsync(AddPackageItemRequest request, ReplacePackageItemRequestArgs query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
           if (request == null) throw new ArgumentNullException(nameof(request));
           if(request.OrganizationProductId <= 0) throw new ArgumentException("Invalid OrganizationProductId", nameof(request.OrganizationProductId));
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.OrganizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.AddItemToPackageAsync(query.PackageId, request);
        }
        public async Task<bool> RemoveAdditionalCostFromPackageAsync(ReplacePackageItemRequestArgs query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            if (query.AdditionalCostId <= 0) throw new ArgumentException("Invalid Additional Cost", nameof(query.InputId));
            return await _inputFinancingRepository.RemoveAdditionalCostFromPackageAsync(query.PackageId, query.AdditionalCostId);
        }

        public async Task<bool> AddAdditionalCostToPackageAsync(AddAdditionalCostRequest request, ReplacePackageItemRequestArgs query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
           
            if (request == null) throw new ArgumentNullException(nameof(request));
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.OrganizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.AddAdditionalCostToPackageAsync(query.PackageId, request);
        }

        public async Task<bool> UpdateAdditionalCostAsync(
  UpdateAdditionalCostRequest request,
  UpdateAdditionalCostArgs query)
        { 
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            if (query.AdditionalCostId <= 0) throw new ArgumentException("Invalid Additional Cost Id", nameof(query.AdditionalCostId));
           if (request == null) throw new ArgumentNullException(nameof(request));
           
            
            return await _inputFinancingRepository.UpdateAdditionalCostAsync(request, query);
        }


        #region Subscription
        public async Task<bool> AddSubscriberAsync(AddSubscriberRequest request, InputSubscriptionRequestArgs query)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(request.PackageId));
            if (string.IsNullOrEmpty(request.SubscriberUserId)) throw new ArgumentException("Invalid Subscriber selected", nameof(request.SubscriberUserId));

            if (!Enum.IsDefined(typeof(InputSubScriptionType), request.SubscriberType))
            {
                throw new BaseException(ExceptionType.BAD_REQUEST,"Invalid subscriber type");
            }

            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.OrganizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            var subscriberExists = await _onBoardingManager.GetUseridentity(request.SubscriberUserId);

            if (subscriberExists == null)
            {
                throw new BaseException(ExceptionType.NOTFOUND,"Subscriber does not exist");
            }
            return await _inputFinancingRepository.AddSubscriberAsync(request, query);
        }
        public async Task<bool> ApproveSubscriptionAsync(ApproveSubscriptionRequest request, InputSubscriptionRequestArgs query)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (query.SubscriptionId <= 0) throw new ArgumentException("Invalid Subscription", nameof(query.SubscriptionId));

            if (!Enum.IsDefined(typeof(ApprovalDecision), request.ApprovalState))
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid Approval State");
            }
            if(string.IsNullOrEmpty(query.SubscriberUserId)) {
                throw new ArgumentException("Invalid subscriber", nameof(query.SubscriberUserId));
            }
            request.SubscriptionId=query.SubscriptionId;
            return await _inputFinancingRepository.ApproveSubscriptionAsync(request, query);
        }

        public async Task<PagedResult<SubscriptionDetailDto>> GetSubscriptionsAsync(
  SubscriptionQueryFilter filter,
  int pageNumber,
  int pageSize)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(filter.OrganizationCode);
            if (hasOrg != null)
            {
                filter.OrganizationId = hasOrg.OrganizaionId;
            }
            if (!filter.ViewType.HasValue)
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "View type is required");
            }
            if (!Enum.IsDefined(typeof(SubscriptionView), filter.ViewType))
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid Subscription view type");
            }

            return await _inputFinancingRepository.GetSubscriptionsAsync(filter, pageNumber, pageSize);
        }
        public async Task<SubscriptionFullDetailDto> FetchSubscriptionForReview(SubscriptionQueryFilter filter)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(filter.OrganizationCode);
            if (hasOrg != null)
            {
                filter.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.FetchSubscriptionForReview(filter);
        }
        
        public async Task<SubscriptionReviewResponseDto> GetSubscriptionDetail(SubscriptionQueryFilter filter)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(filter.OrganizationCode);
            if (hasOrg != null)
            {
                filter.OrganizationId = hasOrg.OrganizaionId;
            }
            if (!filter.ViewType.HasValue)
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "View type is required");
            }
            if (!Enum.IsDefined(typeof(SubscriptionView), filter.ViewType))
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid Subscription view type");
            }

            return await _inputFinancingRepository.GetSubscriptionDetail(filter);
        }
        public async Task<(byte[] FileBytes, string FileName)> DownloadCommissionLetterAsync(long subscriptionId) { 
            return await _inputFinancingRepository.DownloadCommissionLetterAsync(subscriptionId);
        }
        
        public async Task<bool> CommitmentPaymentAsync(
      RecordCommitmentPaymentRequest request,
      InputSubscriptionRequestArgs args)
        {
            if (request == null)
                throw new Exception("Invalid request");
            request.SubscriptionId = args.SubscriptionId;
            request.SubscriberUserId = args.SubscriberUserId;
            if (request.SubscriptionId <= 0) throw new Exception("Subscription Id required");
            if (string.IsNullOrEmpty(request.SubscriberUserId)) throw new Exception("Subscriber User Id required");
           // var strategy = _context.Database.CreateExecutionStrategy();

            //return await strategy.ExecuteAsync(async () =>
            //{
            //    await using var transaction =
            //        await _context.Database.BeginTransactionAsync(
            //            IsolationLevel.Serializable);

                try
                {
                    var subscription = await _context.InputPackageSubscriptions
                        .Include(x => x.InputPackage)
                        .ThenInclude(x=>x.Organization)
                        .Include(x=>x.SubscriberUser)
                        .FirstOrDefaultAsync(x =>
                            x.Id == request.SubscriptionId &&
                            x.SubscriberUserId == args.SubscriberUserId);

                    if (subscription == null)
                        throw new Exception("Subscription not found");

                    if (!subscription.InputPackage.CommitmentRequired)
                        throw new Exception("Commitment not required");

                    if (subscription.Status == SubscriptionStatus.PendingDisbursement)
                        throw new Exception("Already completed commitment stage");

                    var outstanding =
                        subscription.CommitmentValue -
                        await _inputFinancingRepository
                            .GetTotalCommitmentPaidAsync(subscription.Id);

                    if (outstanding <= 0)
                        throw new Exception("Already fully paid");

                    var amountToPay =
                        request.Amount > 0 ? request.Amount : outstanding;

                    amountToPay = Math.Round(amountToPay, 2);

                    if (amountToPay > outstanding)
                        throw new Exception("Amount exceeds outstanding balance");

                    // 1. CREATE PENDING PAYMENT (IMPORTANT)
                  
                    WalletTransferResponse walletResponse = null;
                    if (subscription.InputPackage == null)
                    {
                        throw new Exception("Cannot find input package");
                    }
                    // 2. CHANNEL HANDLING ONLY (NO DB FINALIZATION YET)
                    switch (request.PaymentChannel)
                    {
                        case PaymentChannel.Wallet:
                            walletResponse = await ProcessWalletTransferAsync(
                                request,
                                args,
                                subscription,
                                amountToPay, 
                                "Payment for Commitment on input subscription");

                            break;

                     
                        //case PaymentChannel.OffilineBankTransfer:
                        //    // mark pending reconciliation
                        //    await _inputFinancingRepository.FlagPendingBankTransferAsync(paymentId);
                        //    break;

                        //case PaymentChannel.Cash:
                        //    // must be verified by agent
                        //    await _inputFinancingRepository.FlagPendingCashApprovalAsync(paymentId);
                        //    break;
                        default: throw new Exception("Channel Not supported");
                    }

                  //  await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                  //  await transaction.RollbackAsync();
                    throw;
                }
            //});
        }
      
        
        private async Task<WalletProviderParameterModel[]> RetrieveParams(string WalletProviderCode, string Env)
        {
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == WalletProviderCode).FirstOrDefault().ProviderId;
            WalletProviderParameterModel[] parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            return parameters;

        }
        private async Task<WalletTransferResponse> ProcessWalletTransferAsync(
    RecordCommitmentPaymentRequest request,
    InputSubscriptionRequestArgs args,
    InputPackageSubscription subscription,
    decimal amountToPay,
    string description)
        {
            // ─── Step 1: Validate wallets ───

            var subscriberWallet = await _userWalletRepository.GetUserWalletByProviderId(
                new UserWalletParams
                {
                    Id = request.WalletProviderId,
                    UserId = request.SubscriberUserId,
                    businessCode = args.BusinessCode
                });

            if (subscriberWallet == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Subscriber wallet not found");

            var organizationWalletProviderId = subscription.InputPackage.WalletProviderId;

            if (!organizationWalletProviderId.HasValue)
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "Payment operation cannot be performed. Contact your organization");

            var organizationWallet = await _userWalletRepository.GetUserWalletByProviderId(
                new UserWalletParams
                {
                    Id = organizationWalletProviderId.Value,
                    UserId = subscription.InputPackage.Organization.UserId,
                    businessCode = args.BusinessCode
                });

            if (organizationWallet == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Organization wallet not found");

            // ─── Step 2: Balance check ───

            if (subscriberWallet.WalletBalance < amountToPay)
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    $"Insufficient wallet balance. Kindly top up at least ₦{amountToPay:N0}");

            // ─── Step 3: Generate references ───

            var masterReference = !string.IsNullOrWhiteSpace(request.Reference)
                ? request.Reference
                : $"{Guid.NewGuid():D}";

            var debitReference = $"{masterReference}";
            var creditReference = $"{Guid.NewGuid():D}";

            // ─── Step 4: Resolve provider ───

            var provider = await _providerRepo.Get(subscriberWallet.WalletProviderId);

            if (provider == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Wallet provider not found");



            var providerType = provider.Code.ToEnum<WalletProviderType>();

            var providerParams = await RetrieveParams(provider.Code, args.Env);
            var providerService = _factory.GetService(providerType);


            if (providerService == null)
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    $"Wallet service for {provider.Code} was not found");

            // ─── Step 5: Use ExecutionStrategy to handle retryable operations with transactions ───
            //     This ensures SqlServerRetryingExecutionStrategy is properly applied

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create the pending payment record inside the ambient transaction
                    var paymentId = await _inputFinancingRepository
                        .CreatePendingPaymentAsync(new CreateCommitmentPaymentRequest
                        {
                            SubscriptionId = subscription.Id,
                            Amount = amountToPay,
                            PaymentChannel = request.PaymentChannel,
                            Reference = masterReference,
                            WalletProviderId = request.WalletProviderId,
                            RecordedByUserId = args.UserId
                        });

                    // Initiate the debit record


                    await _walletTransactionRepository.InitiateWalletWithdrawal(
                        subscriberWallet.Id, amountToPay, description, $"{debitReference}-DB");

                    // ─── Step 6: Execute debit (external call — outside DB transaction scope) ───

                    bool debitSuccessful = false;
                    string? debitProviderReference = null;
                    string providerMessage = "";
                    try
                    {
                        var debitRequest = new WalletTopUpProviderRequest
                        {
                            WalletNumber = subscriberWallet.WalletNumber,
                            Amount = amountToPay,
                            Currency = subscriberWallet.Currency,
                            Message = $"Approve withdrawal of {amountToPay:N0} for input commitment payment",
                            TransactionReference = debitReference
                        };

                        if (provider.Code == "Momo")
                        {
                            var debitResponse = await _mtnMomoService.DebitWalletAsync(
                                debitRequest, providerParams, args.Env);

                            if (debitResponse == null)
                            {
                                await _walletTransactionRepository.CompleteWalletWithdrawal(
                                    $"{debitReference}-DB", false, null, "Wallet debit request returned no response");

                                throw new BaseException(
                                    ExceptionType.OPERATION_FAILED,
                                    "Could not initiate wallet debit");
                            }

                            var debitStatus = await _mtnMomoService.GetRequesToWithdrawStatus(
                              debitReference, providerParams, args.Env);

                            if (debitStatus == null || !debitStatus.IsSuccess)
                            {
                                var debitFailMessage = debitStatus?.Message ?? "Wallet debit failed";
                                
                                await _walletTransactionRepository.CompleteWalletWithdrawal(
                                      $"{debitReference}-DB", false, null, debitFailMessage);

                                throw new BaseException(ExceptionType.OPERATION_FAILED, debitFailMessage);
                            }
                            providerMessage = debitResponse.Data.Status;
                            debitProviderReference = debitResponse.Data.Reference;
                        }
                        else
                        {
                            throw new BaseException(
                                ExceptionType.OPERATION_FAILED,
                                $"Debit not implemented for provider {provider.Code}");
                        }

                        debitSuccessful = true;

                        await _walletTransactionRepository.CompleteWalletWithdrawal(
                            $"{debitReference}-DB", true, debitProviderReference, providerMessage);
                    }
                    catch (BaseException)
                    {
                        // Debit failed — roll back the entire DB transaction cleanly
                        await dbTransaction.RollbackAsync();
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await _walletTransactionRepository.CompleteWalletWithdrawal(
                            $"{debitReference}-DB", false, null, ex.Message);

                        await dbTransaction.RollbackAsync();

                        throw new BaseException(
                            ExceptionType.OPERATION_FAILED,
                            $"An unexpected error occurred during wallet debit: {ex.Message}");
                    }

                    // ─── Step 7: Execute credit (only reached if debit succeeded) ───

                    await _walletTransactionRepository.CreateWalletFundinAsync(
                        organizationWallet.Id, amountToPay, description, $"{creditReference}-CR");

                    try
                    {
                        var transferRequest = new TransferRequest
                        {
                            AccountNumber = organizationWallet.WalletNumber,
                            Amount = $"{amountToPay}",
                            Currency = organizationWallet.Currency,
                            Message = $"Commitment payment for {subscription.InputPackage.Name} by {subscription.SubscriberUser.Name}",
                            Reference = creditReference
                        };

                        var transferResponse = await providerService.InitiateWalletTransferAsync(
                            transferRequest, providerParams, args.Env);

                        if (transferResponse == null)
                        {
                            await _walletTransactionRepository.CompleteWalletFunding(
                                 $"{creditReference}-CR", false, null, "Wallet transfer returned no response");

                            // Debit already succeeded externally — commit what we have so the
                            // reconciliation flag is persisted, then throw.
                            await dbTransaction.CommitAsync();

                            await _walletTransactionRepository.FlagForReconciliation(
                                 $"{debitReference}-DB", $"{creditReference}-CR",
                                "Credit returned no response after successful debit");

                            throw new BaseException(
                                ExceptionType.OPERATION_FAILED,
                                "Wallet service did not return a response. Our team has been notified to reconcile your transaction.");
                        }

                        if (provider.Code == "Momo")
                        {
                            var transferStatus = await _mtnMomoService.GetTransferStatus(
                                 creditReference, providerParams, args.Env);

                            if (transferStatus == null || !transferStatus.IsSuccess)
                            {
                                var creditFailMessage = transferStatus?.Message ?? "Wallet transfer failed";

                                await _walletTransactionRepository.CompleteWalletFunding(
                                    $"{creditReference}-CR", false, null, creditFailMessage);

                                // Commit partial state so reconciliation flag persists
                                await dbTransaction.CommitAsync();

                                await _walletTransactionRepository.FlagForReconciliation(
                                    $"{debitReference}-DB", $"{creditReference}-CR",
                                    $"Credit failed after successful debit: {creditFailMessage}");

                                throw new BaseException(
                                    ExceptionType.OPERATION_FAILED,
                                    "Payment was debited but could not be credited to the organization. Our team has been notified to reconcile your transaction.");
                            }
                        }
                        else
                        {
                            await dbTransaction.RollbackAsync();

                            throw new BaseException(
                                ExceptionType.OPERATION_FAILED,
                                $"Credit not implemented for provider {provider.Code}");
                        }

                        // ─── Step 8: Finalise — all external calls succeeded ───

                        await _walletTransactionRepository.CompleteWalletFunding(
                            $"{creditReference}-CR", true,
                            transferResponse.Data.Reference,
                            transferResponse.Data.Status);

                        await _inputFinancingRepository
                            .RecordSuccessfulPaymentAsync(paymentId, new PaymentConfirmationRequest
                            {
                                ExternalReference = masterReference,
                                Status = TransactionStatus.Successful
                            });

                        // Commit everything atomically
                        await dbTransaction.CommitAsync();

                        return new WalletTransferResponse
                        {
                            IsSuccessful = true,
                            Amount = amountToPay,
                            ExternalReference = masterReference,
                            ProviderTransactionId = $"{creditReference}-CR"
                        };
                    }
                    catch (BaseException)
                    {
                        // Already handled (committed or rolled back) above — just rethrow
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await _walletTransactionRepository.CompleteWalletFunding(
                            creditReference, false, null, ex.Message);

                        // Commit the debit record so the reconciliation flag is durable
                        await dbTransaction.CommitAsync();

                        await _walletTransactionRepository.FlagForReconciliation(
                            debitReference, creditReference,
                            $"Unexpected error during credit after successful debit: {ex.Message}");

                        throw new BaseException(
                            ExceptionType.OPERATION_FAILED,
                            "An unexpected error occurred while crediting the organization. Our team has been notified to reconcile your transaction.");
                    }
                }
                catch (BaseException)
                {
                    // If the transaction is still open (wasn't committed/rolled back inside),
                    // roll it back now before rethrowing.
                    if (_context.Database.CurrentTransaction != null)
                        await dbTransaction.RollbackAsync();

                    throw;
                }
                catch (Exception ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        await dbTransaction.RollbackAsync();

                    throw new BaseException(
                        ExceptionType.OPERATION_FAILED,
                        $"An unexpected error occurred: {ex.Message}");
                }
            });
        }

        /**  private async Task<WalletTransferResponse> ProcessWalletTransferAsync(
        RecordCommitmentPaymentRequest request,
        InputSubscriptionRequestArgs args,
        InputPackageSubscription subscription,
        decimal amountToPay,
        string description)
          {

              var subscriberWallet = await _userWalletRepository.GetUserWalletByProviderId(
                  new UserWalletParams
                  {
                      Id = request.WalletProviderId,
                      UserId = request.SubscriberUserId,
                      businessCode = args.BusinessCode
                  });

              if (subscriberWallet == null)
                  throw new BaseException(ExceptionType.NOTFOUND, "Subscriber wallet not found");

              var organizationWalletProviderId = subscription.InputPackage.WalletProviderId;

              if (!organizationWalletProviderId.HasValue)
                  throw new BaseException(
                      ExceptionType.OPERATION_FAILED,
                      "Payment operation cannot be performed. Contact your organization");

              var organizationWallet = await _userWalletRepository.GetUserWalletByProviderId(
                  new UserWalletParams
                  {
                      Id = organizationWalletProviderId.Value,
                      UserId = subscription.InputPackage.Organization.UserId,
                      businessCode = args.BusinessCode
                  });

              if (organizationWallet == null)
                  throw new BaseException(ExceptionType.NOTFOUND, "Organization wallet not found");


              if (subscriberWallet.WalletBalance < amountToPay)
                  throw new BaseException(
                      ExceptionType.OPERATION_FAILED,
                      $"Insufficient wallet balance. Kindly top up at least ₦{amountToPay:N0}");


              var paymentId = await _inputFinancingRepository
                        .CreatePendingPaymentAsync(new CreateCommitmentPaymentRequest
                        {
                            SubscriptionId = subscription.Id,
                            Amount = amountToPay,
                            PaymentChannel = request.PaymentChannel,
                            Reference = request.Reference,
                            WalletProviderId = request.WalletProviderId,
                            RecordedByUserId=args.UserId
                        });

              var masterReference = !string.IsNullOrWhiteSpace(request.Reference)
                  ? request.Reference
                  : $"COMMITMENT-{Guid.NewGuid():N}";

              var debitReference = $"{masterReference}-DB";
              var creditReference = $"{masterReference}-CR";

              
              var provider = await _providerRepo.Get(subscriberWallet.WalletProviderId);

              if (provider == null)
                  throw new BaseException(ExceptionType.NOTFOUND, "Wallet provider not found");

              var providerParams = await RetrieveParams(provider.Code, args.Env);
              var providerType = provider.Code.ToEnum<WalletProviderType>();
              var providerService = _factory.GetService(providerType);

              if (providerService == null)
                  throw new BaseException(
                      ExceptionType.OPERATION_FAILED,
                      $"Wallet service for {provider.Code} was not found");


              await _walletTransactionRepository.InitiateWalletWithdrawal(
                  subscriberWallet.Id, amountToPay, description, debitReference);


              bool debitSuccessful = false;
              string? debitProviderReference = null;

              try
              {
                  var debitRequest = new WalletTopUpProviderRequest
                  {
                      WalletNumber = subscriberWallet.WalletNumber,
                      Amount = amountToPay,
                      Currency = subscriberWallet.Currency,
                      Message = $"Approved withdrawal of {amountToPay:N0} for input commitment payment",
                      TransactionReference = debitReference
                  };

                  if (provider.Code == "Momo")
                  {
                      var debitResponse = await _mtnMomoService.DebitWalletAsync(
                          debitRequest, providerParams, args.Env);

                      if (debitResponse == null)
                      {
                          await _walletTransactionRepository.CompleteWalletWithdrawal(
                              debitReference, false, null, "Wallet debit request returned no response");

                          throw new BaseException(
                              ExceptionType.OPERATION_FAILED,
                              "Could not initiate wallet debit");
                      }

                      var debitStatus = await _mtnMomoService.GetRequesToWithdrawStatus(
                          debitReference, providerParams, args.Env);

                      if (debitStatus == null || !debitStatus.IsSuccess)
                      {
                          var debitFailMessage = debitStatus?.Message ?? "Wallet debit failed";

                          await _walletTransactionRepository.CompleteWalletWithdrawal(
                              debitReference, false, null, debitFailMessage);

                          throw new BaseException(
                              ExceptionType.OPERATION_FAILED,
                              debitFailMessage);
                      }

                      debitProviderReference = debitResponse.Data.Reference;
                  }
                  else
                  {
                      throw new BaseException(
                          ExceptionType.OPERATION_FAILED,
                          $"Debit not implemented for provider {provider.Code}");
                  }

                  debitSuccessful = true;

                  await _walletTransactionRepository.CompleteWalletWithdrawal(
                      debitReference, true, debitProviderReference, "Debit successful");
              }
              catch (BaseException)
              {
                  throw;
              }
              catch (Exception ex)
              {
                  await _walletTransactionRepository.CompleteWalletWithdrawal(
                      debitReference, false, null, ex.Message);

                  throw new BaseException(
                      ExceptionType.OPERATION_FAILED,
                      $"An unexpected error occurred during wallet debit: {ex.Message}");
              }


              await _walletTransactionRepository.CreateWalletFundinAsync(
                  organizationWallet.Id, amountToPay, description, creditReference);

              try
              {
                  var transferRequest = new TransferRequest
                  {
                      AccountNumber = organizationWallet.WalletNumber,
                      Amount = $"{amountToPay}",
                      Currency = organizationWallet.Currency,
                      Message = $"Commitment payment for {subscription.InputPackage.Name} by {subscription.SubscriberUser.Name}",
                      Reference = creditReference
                  };

                  var transferResponse = await providerService.InitiateWalletTransferAsync(
                      transferRequest, providerParams, args.Env);

                  if (transferResponse == null)
                  {
                      await _walletTransactionRepository.CompleteWalletFunding(
                          creditReference, false, null, "Wallet transfer returned no response");

                      await _walletTransactionRepository.FlagForReconciliation(
                          debitReference, creditReference,
                          "Credit returned no response after successful debit");

                      throw new BaseException(
                          ExceptionType.OPERATION_FAILED,
                          "Wallet service did not return a response. Our team has been notified to reconcile your transaction.");
                  }

                  if (provider.Code == "Momo")
                  {
                      var transferStatus = await _mtnMomoService.GetTransferStatus(
                          creditReference, providerParams, args.Env);

                      if (transferStatus == null || !transferStatus.IsSuccess)
                      {
                          var creditFailMessage = transferStatus?.Message ?? "Wallet transfer failed";

                          await _walletTransactionRepository.CompleteWalletFunding(
                              creditReference, false, null, creditFailMessage);
await _walletTransactionRepository.FlagForReconciliation(
                              debitReference, creditReference,
                              $"Credit failed after successful debit: {creditFailMessage}");

                          throw new BaseException(
                              ExceptionType.OPERATION_FAILED,
                              "Payment was debited but could not be credited to the organization. Our team has been notified to reconcile your transaction.");
                      }
                  }
                  else
                  {
                      throw new BaseException(
                          ExceptionType.OPERATION_FAILED,
                          $"Credit not implemented for provider {provider.Code}");
                  }

                  await _walletTransactionRepository.CompleteWalletFunding(
                      creditReference, true,
                      transferResponse.Data.Reference,
                      transferResponse.Data.Status);

                  await _inputFinancingRepository
                      .RecordSuccessfulPaymentAsync(paymentId, new PaymentConfirmationRequest
                      {
                          ExternalReference = masterReference,
                          Status = TransactionStatus.Successful
                      });


                  return new WalletTransferResponse
                  {
                      IsSuccessful = true,
                      Amount = amountToPay,
                      ExternalReference = masterReference,
                      ProviderTransactionId = creditReference
                  };
              }
              catch (BaseException)
              {
                  throw;
              }
              catch (Exception ex)
              {
                  await _walletTransactionRepository.CompleteWalletFunding(
                      creditReference, false, null, ex.Message);

                  await _walletTransactionRepository.FlagForReconciliation(
                      debitReference, creditReference,
                      $"Unexpected error during credit after successful debit: {ex.Message}");

                  throw new BaseException(
                      ExceptionType.OPERATION_FAILED,
                      "An unexpected error occurred while crediting the organization. Our team has been notified to reconcile your transaction.");
              }
          
          
          }
          **/

        public async Task SendCommitmentReminderAsync(SubscriptionQueryFilter filter)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(filter.OrganizationCode);
            if (hasOrg != null)
            {
                filter.OrganizationId = hasOrg.OrganizaionId;
            }
            var reminder = await _inputFinancingRepository.GetCommitmentReminderAsync(filter.SubscriptionId);

            if (reminder == null || !reminder.RequiresReminder)
                return;
            var senderEmail = _configuration["Official:Email"];
            var otpTarget = !string.IsNullOrWhiteSpace(reminder.Email)
                ? OtpTarget.Email
                : OtpTarget.Phone;

            var destination = otpTarget == OtpTarget.Email
                ? reminder.Email
                : reminder.PhoneNumber;

            if (otpTarget == OtpTarget.Email)
            {
                //use aws
                await _awsService.SendEmailAsync(
                    senderEmail,
                    destination,
                    "Commitment Payment Reminder",
                   $"{reminder.Message}",
                   $"{reminder.Message}"
                    );

            }
            else
            {
                await _sMSService.SendSmsAsync(
                        destination,
                        $"{reminder.Message}");
            }
            await _awsService.SendEmailAsync(
                   senderEmail,
                   "agrovesto@gmail.com",
                   "Commitment Payment Reminder",
                   $"You've been copied on the details below for Organization Input Financing of '{hasOrg.OrganizationName}' '{reminder.Message}'",
                   $"You've been copied on the details below for Organization Input Financing of '{hasOrg.OrganizationName}' '{reminder.Message}'"                 
                   );

            
        }

        public async Task<InputFinancingDashboardDto> GetInputFinancingDashboardAsync(
         SubscriptionQueryFilter filter)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(filter.OrganizationCode);
            if (hasOrg != null)
            {
                filter.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.GetInputFinancingDashboardAsync(filter);
        }
        public async Task<byte[]> ExportSubscriptionsAsync(SubscriptionQueryFilter filter, ExportFormat format)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(filter.OrganizationCode);
            if (hasOrg != null)
            {
                filter.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.ExportSubscriptionsAsync(filter,format);
        }

        public async Task<PagedResult<InputObligationDto>> GetFarmerInputObligations(
      SubscriptionQueryFilter filter,
      int pageNumber,
      int pageSize)
        {
            return await _inputFinancingRepository.GetSubscriptionObligationsAsync(filter, pageNumber, pageSize);

        }
        public async Task<LoanDetailDto> GetLoanDetailAsync(ListQueryParams args)
        {
            return await _inputFinancingRepository.GetLoanDetailsAsync(args.Id);
        }
        public async Task<PagedResult<FinancingBreakdownDto>> GetFinancingBreakdownAsync(ListQueryParams args)
        {
            return await _inputFinancingRepository.GetFinancingBreakdownAsync(args.FarmerUserId, args.PageNumber, args.PageSize);
        }
        public async Task<InputLoanSummaryDto> GetInputLoanSummaryAsync(ListQueryParams args)
        {
            return await _inputFinancingRepository.GetInputLoanSummaryAsync(args.FarmerUserId);

        }
        public async Task<PagedResult<RepaymentBreakdownDto>> GetRepaymentBreakdownAsync(
      long subscriptionId,
      ListQueryParams args)
        {
           

                return await _inputFinancingRepository.GetRepaymentBreakdownAsync(subscriptionId, args.PageNumber, args.PageSize);
          
        }
        public async Task<FinancingRepaymentSummaryDto> GetFinancingRepaymentSummaryAsync(ListQueryParams args)
        {
        
                return await _inputFinancingRepository.GetFinancingRepaymentSummaryAsync(args.Id);
           
        }



        #endregion

        #region reporting

        #endregion
    }
}
