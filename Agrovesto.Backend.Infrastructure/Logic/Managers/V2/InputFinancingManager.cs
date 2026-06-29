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
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Rekognition.Model;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using Microsoft.AspNetCore.Http;
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
        Task<bool> ApproveInputDisbursementRequestAsync(ApproveDisbursementRequestDto request, string approvedByUserId);
        Task<DisburseSubscriptionResult> DisburseSubscriptionAsync(DisburseSubscriptionRequest request, InputSubscriptionRequestArgs args);
        Task<long> RequestInputDisbursementApprovalAsync(InputSubscriptionRequestArgs args);
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
        private readonly IAWSRekognitionManager _imageRecognitionService;
        private readonly IFarmerRepository _farmerRepository;

        private readonly IRepaymentEngine _repaymentEngine;

        public InputFinancingManager(IWalletServiceFactory factory, IWalletProviderRepository providerRepo, IUserWalletRepository userWalletRepository,
            IInputFinancingRepository inputFinancingRepository, IBusinessRepository businessRepository,
            IWalletProviderRepository walletProviderRepository, IOnBoardingManager onBoardingManager, IAWSService aWSService,
            IConfiguration configuration, INotificationService notificationService,
            DataContext context, IMTNMomo mtnMomoService,
            IAWSRekognitionManager imageRecognitionService,
            IUserWalletRepository walletTransactionRepository,
            IFarmerRepository farmerRepository,
            IRepaymentEngine repaymentEngine
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
            _context = context;
            _mtnMomoService = mtnMomoService;
            _factory = factory;
            _imageRecognitionService = imageRecognitionService;
            _farmerRepository = farmerRepository;
            _repaymentEngine= repaymentEngine;
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
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            if (query.InputId <= 0) throw new ArgumentException("Invalid InputId", nameof(query.InputId));
            return await _inputFinancingRepository.RemoveItemFromPackage(query.PackageId, query.InputId);
        }

        public async Task<bool> ReplacePackageItemAsync(ReplacePackageItemRequest request, ReplacePackageItemRequestArgs query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            if (query.InputId <= 0) throw new ArgumentException("Invalid InputId", nameof(query.InputId));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.OrganizationProductId <= 0) throw new ArgumentException("Invalid OrganizationProductId", nameof(request.OrganizationProductId));
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
            if (request.OrganizationProductId <= 0) throw new ArgumentException("Invalid OrganizationProductId", nameof(request.OrganizationProductId));
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
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid subscriber type");
            }

            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.OrganizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            var subscriberExists = await _onBoardingManager.GetUseridentity(request.SubscriberUserId);

            if (subscriberExists == null)
            {
                throw new BaseException(ExceptionType.NOTFOUND, "Subscriber does not exist");
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
            if (string.IsNullOrEmpty(query.SubscriberUserId))
            {
                throw new ArgumentException("Invalid subscriber", nameof(query.SubscriberUserId));
            }
            request.SubscriptionId = query.SubscriptionId;
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
        public async Task<(byte[] FileBytes, string FileName)> DownloadCommissionLetterAsync(long subscriptionId)
        {
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
                    .ThenInclude(x => x.Organization)
                    .Include(x => x.SubscriberUser)
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
                            }, args.UserId);



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
            return await _inputFinancingRepository.ExportSubscriptionsAsync(filter, format);
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

        public async Task<bool> ApproveInputDisbursementRequestAsync(
          ApproveDisbursementRequestDto request,
          string approvedByUserId)
        {
            return await _inputFinancingRepository.ApproveInputDisbursementRequestAsync(request, approvedByUserId);
        }
        public async Task<long> RequestInputDisbursementApprovalAsync(
          InputSubscriptionRequestArgs args)
        {
            var reference = ReferenceUtil.GenerateDisbursementReference();
            return await _inputFinancingRepository.RequestInputDisbursementApprovalAsync(args.SubscriptionId, reference, args.UserId);
        }

        public async Task<DisburseSubscriptionResult> DisburseSubscriptionAsync(
            DisburseSubscriptionRequest request,
            InputSubscriptionRequestArgs args)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ── 1. Validate args ──────────────────────────────────────────────
                    if (string.IsNullOrEmpty(args.SubscriberUserId))
                        throw new DisbursementException("Subscriber user is required.");

                    if (args.SubscriptionId <= 0)
                        throw new DisbursementException("A valid Subscription Id is required.");

                  
                    if (request.Latitude == 0 && request.Longitude == 0)
                        throw new DisbursementException(
                            "Geolocation is required for disbursement. " +
                            "Ensure location permissions are granted on your device.");

                    ValidateLiveImage(request.UserImage);

                    var subscription = await _context.InputPackageSubscriptions
                        .Include(x => x.InputPackage)
                            .ThenInclude(x => x.InputPackageItems)
                        .Include(x => x.InputPackage)
                            .ThenInclude(x => x.RepaymentPlan)
                        .Include(x => x.InputPackage)
                            .ThenInclude(x => x.AdditionalCosts)
                        .Include(x => x.SubscriberUser)
                        .Include(x => x.Organization)
                        .FirstOrDefaultAsync(x =>
                            x.Id == args.SubscriptionId &&
                            x.SubscriberUserId == args.SubscriberUserId);

                    if (subscription == null)
                        throw new DisbursementException("Subscription not found.");

                    if (subscription.SubscriberUser == null)
                        throw new DisbursementException("Subscriber not found. Please try again later.");

                   
                    if (subscription.Status != SubscriptionStatus.PendingDisbursement)
                        throw new DisbursementException("Subscription is not in PendingDisbursement status.");

                    if (subscription.InputPackage.RepaymentPlan == null)
                        throw new DisbursementException("Repayment plan is required before disbursement.");

                    var user = await _onBoardingManager.GetUseridentity(subscription.SubscriberUser.Id);
                    if (user == null)
                        throw new DisbursementException("User details not found.");

                    if (request.NIN != user.NIN)
                        throw new DisbursementException(
                            "Supplied NIN does not match our records. " +
                            "Kindly update the subscriber's profile.");

                    var farmerImage = await _farmerRepository.GetFarmerImage(
                        subscription.SubscriberUser.Id,
                        args.BusinessCode,
                        subscription.Organization.OrganizationCode);

                    if (farmerImage == null)
                        throw new DisbursementException(
                            "No KYC photo on record for this subscriber. Update the user's KYC photo.");

                    var faceResult = await _imageRecognitionService.CompareFaces(
                        request.UserImage,
                        new ImageVerificationArgs
                        {
                            BusinessCode = args.BusinessCode,
                            FarmerUserId = subscription.SubscriberUser.Id
                        });

                    if (faceResult == null || !faceResult.IsSimilarFace)
                        throw new DisbursementException(
                            $"Face verification failed " +
                            $"(similarity: {faceResult?.SimilarityPercentage ?? 0:P0}). " +
                            "Disbursement is not allowed.");

                    var agent = await _context.Agents
                        .FirstOrDefaultAsync(x => x.UserId == args.UserId);

                    if (agent == null)
                        throw new DisbursementException("No agent profile found for the current user.");

                    var organizationAgent = await _context.OrganizationAgents
                        .Include(x => x.Location)
                        .FirstOrDefaultAsync(x =>
                            x.AgentId == agent.Id &&
                            x.OrganizationId == args.OrganizationId &&
                            x.IsActive);

                    if (organizationAgent == null)
                        throw new DisbursementException(
                            "Agent does not have an active assignment within this organization " +
                            "and cannot perform disbursements.");

                    if (organizationAgent.Location == null)
                        throw new DisbursementException(
                            "Agent's assigned location could not be resolved. " +
                            "Contact an administrator to correct the assignment.");

                    if (!organizationAgent.Location.IsActive)
                        throw new DisbursementException(
                            $"Location '{organizationAgent.Location.LocationName}' is currently " +
                            "inactive and cannot be used for disbursements.");

                    if (organizationAgent.Location.Id != request.WarehouseId)
                        throw new DisbursementException(
                            "Selected location does not match the agent's assigned location.");

                    var agentLocation = organizationAgent.Location;

                    double distanceMetres = GeometryHelper.HaversineDistanceMetres(
                        lat1: request.Latitude,
                        lon1: request.Longitude,
                        lat2: agentLocation.Latitude,
                        lon2: agentLocation.Longitude);

                    if (distanceMetres > GeometryHelper.AllowedRadiusMetres)
                        throw new DisbursementException(
                            $"Your current location ({distanceMetres:F0} m away) does not match " +
                            $"the assigned inventory location '{agentLocation.LocationName}'. " +
                            "Move to the correct location and try again.");

                    var approvedRequest = await _context.InputSubscriptionDisbursements
                        .FirstOrDefaultAsync(x =>
                            x.SubscriptionId == args.SubscriptionId &&
                            x.Status == DisbursementStatus.Approved);

                    if (approvedRequest == null)
                        throw new DisbursementException("No approved disbursement request found.");

                    var alreadyDisbursed = await _context.InputSubscriptionDisbursements
                        .AnyAsync(x =>
                            x.SubscriptionId == subscription.Id &&
                            x.Status == DisbursementStatus.Disbursed);

                    if (alreadyDisbursed)
                        throw new DisbursementException("This subscription has already been fully disbursed.");

                    var reference = ReferenceUtil.GenerateDisbursementReference();

                    var additionalCosts = subscription.InputPackage.AdditionalCosts
                        ?? new List<AdditionalInputPackageCost>();

                    decimal paidToUserTotal = additionalCosts
                        .Where(x => x.AdditionalCostTreatmentType == AdditionalCostTreatmentType.PaidOutToUser)
                        .Sum(x => x.Amount);

                    decimal vendorPaidTotal = additionalCosts
                        .Where(x => x.AdditionalCostTreatmentType == AdditionalCostTreatmentType.VendorPaid)
                        .Sum(x => x.Amount);

                    decimal disbursedAmount = 0m;
                    bool isPartial = false;
                    var lineItems = new List<DisbursementLineItem>();

                    if (subscription.InputPackage.PackageStructure == InputPackageStructure.Physical)
                    {
                        disbursedAmount = await ProcessPhysicalDisbursementAsync(
                            subscription, agentLocation, approvedRequest,
                            request, args, reference,
                            paidToUserTotal, vendorPaidTotal,
                            lineItems,  isPartial);
                    }
                    else
                    {
                        disbursedAmount = await ProcessMonetaryDisbursementAsync(
                            subscription, agentLocation, approvedRequest,
                            request, args, reference,
                            isPartial);
                    }

                    if (lineItems.Count > 0)
                        _context.DisbursementLineItems.AddRange(lineItems);

                    approvedRequest.Status = DisbursementStatus.Disbursed;
                    approvedRequest.DisbursedOn = DateTimeOffset.UtcNow;
                    approvedRequest.ApprovedAmount = disbursedAmount;
                    approvedRequest.DisbursedByUserId = args.UserId;
                    approvedRequest.Reference = reference;          // use generated ref
                    approvedRequest.IsPartial = isPartial;
                    approvedRequest.LocationId = agentLocation.Id;
                    approvedRequest.Latitude = request.Latitude;
                    approvedRequest.Longitude = request.Longitude;

                    subscription.Status = isPartial
                        ? SubscriptionStatus.PartiallyDisbursed
                        : SubscriptionStatus.Active;
                    subscription.DisbursedOn = DateTimeOffset.UtcNow;

                    var plan = subscription.InputPackage.RepaymentPlan;
                    var schedule = _repaymentEngine.GenerateSchedule(new RepaymentEngineRequest
                    {
                        InputSubscriptionId = subscription.Id,
                        Frequency = plan.RepaymentFrequency,
                        Duration = plan.RepaymentDuration,
                        StartDate = plan.RepaymentStartDate,
                        EndDate = plan.RepaymentEndDate,
                        GracePeriod = plan.RepaymentGracePeriod,
                        TotalAmount = disbursedAmount
                    });

                    _context.RepaymentSchedules.AddRange(schedule.Select(x => new RepaymentSchedule
                    {
                        InputPackageSubscriptionId = subscription.Id,
                        InstallmentNumber = x.InstallmentNumber,
                        DueDate = x.DueDate,
                        Amount = x.Amount,
                        IsWithinGracePeriod = x.IsWithinGracePeriod,
                        IsPaid = false
                    }));

                    _context.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        EventType = nameof(SubscriptionDisbursedEvent),
                        CreatedOn = DateTime.UtcNow,
                        IsProcessed = false,
                        Payload = JsonSerializer.Serialize(new SubscriptionDisbursedEvent
                        {
                            SubscriptionId = subscription.Id,
                            PackageId = subscription.InputPackageId,
                            Amount = disbursedAmount,
                            IsPartial = isPartial,
                            LocationId = agentLocation.Id,
                            AgentUserId = args.UserId,
                            Latitude = request.Latitude,
                            Longitude = request.Longitude,
                        })
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new DisburseSubscriptionResult
                    {
                        Success = true,
                        DisbursedAmount = disbursedAmount,
                        IsPartial = isPartial,
                        LocationName = agentLocation.LocationName,
                        LineItems = lineItems.Select(x => new DisbursementLineItemModel
                        {
                            Id = x.Id,
                            DisbursementId = x.DisbursementId,
                            SubscriptionId = x.SubscriptionId,
                            OrganizationProductId = x.OrganizationProductId,
                            SubscribedQuantity = x.SubscribedQuantity,
                            DisbursedQuantity = x.DisbursedQuantity,
                            PendingQuantity = x.PendingQuantity,
                            UnitCost = x.UnitCost,
                            TotalDisbursedValue = x.TotalDisbursedValue,
                            LocationId = x.LocationId,
                            AgentUserId = x.AgentUserId,
                            Latitude = x.Latitude,
                            Longitude = x.Longitude,
                            CreatedOn = x.CreatedOn,
                        }).ToList()
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }


        // ── Private helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Validates the uploaded live image before any DB calls are made.
        /// Throws <see cref="DisbursementException"/> on any violation.
        /// </summary>
        private static void ValidateLiveImage(IFormFile? image)
        {
            if (image == null)
                throw new DisbursementException("A live image file is required for disbursement.");

            if (image.Length == 0)
                throw new DisbursementException("The uploaded live image file is empty.");

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(image.ContentType?.ToLowerInvariant()))
                throw new DisbursementException(
                    $"Invalid file type '{image.ContentType}'. " +
                    "Only JPEG, PNG, and WebP images are accepted.");

            const long maxBytes = 10 * 1024 * 1024; // 10 MB
            if (image.Length > maxBytes)
                throw new DisbursementException("Live image must not exceed 10 MB.");
        }

        /// <summary>
        /// Handles the physical goods path: checks inventory per item, depletes stock,
        /// records an InventoryTransaction per item, and builds DisbursementLineItems.
        /// Returns the total amount disbursed.
        /// </summary>
        private async Task<decimal> ProcessPhysicalDisbursementAsync(
            InputPackageSubscription subscription,
            OrganizationLocation agentLocation,
            InputSubscriptionDisbursement approvedRequest,
            DisburseSubscriptionRequest request,
            InputSubscriptionRequestArgs args,
            string reference,
            decimal paidToUserTotal,
            decimal vendorPaidTotal,
            List<DisbursementLineItem> lineItems,
            bool isPartial)
        {
            decimal itemsTotal = 0m;

            foreach (var item in subscription.InputPackage.InputPackageItems)
            {
                var inventory = await _context.LocationInventories
                    .Include(x => x.OrganizationProduct)
                    .FirstOrDefaultAsync(x =>
                        x.OrganizationProductId == item.OrganizationProductId &&
                        x.LocationId == agentLocation.Id &&
                        x.LocationInventoryType == LocationInventoryType.Product &&
                        x.IsActive == true &&
                        x.DeletedOn == null);

                if (inventory == null)
                    throw new DisbursementException(
                        $"Product '{item.OrganizationProductId}' is not stocked at " +
                        $"location '{agentLocation.LocationName}'.");

                if (inventory.Quantity == 0)
                    throw new DisbursementException(
                        $"'{inventory.OrganizationProduct.ProductName}' is out of stock " +
                        $"at '{agentLocation.LocationName}'.");

                decimal quantityToGive = Math.Min(item.Quantity, inventory.Quantity);
                decimal quantityPending = item.Quantity - quantityToGive;
                if (quantityPending > 0) isPartial = true;

                decimal quantityBefore = inventory.Quantity;
                inventory.Quantity -= quantityToGive;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    OrganizationProductId = inventory.OrganizationProductId,
                    FromLocationId = agentLocation.Id,
                    Quantity = quantityToGive,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = inventory.Quantity,
                    TransactionType = InventoryTransactionType.Disbursement,
                    ReferenceNumber = reference,
                    SubscriptionId = subscription.Id,
                    TransactionDate = DateTimeOffset.UtcNow,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    PerformedByUserId = args.UserId,
                });

                lineItems.Add(new DisbursementLineItem
                {
                    DisbursementId = approvedRequest.Id,
                    SubscriptionId = subscription.Id,
                    OrganizationProductId = item.OrganizationProductId,
                    SubscribedQuantity = item.Quantity,
                    DisbursedQuantity = quantityToGive,
                    PendingQuantity = quantityPending,
                    UnitCost = item.UnitCost,
                    TotalDisbursedValue = quantityToGive * item.UnitCost,
                    LocationId = agentLocation.Id,
                    AgentUserId = args.UserId,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    CreatedOn = DateTimeOffset.UtcNow,
                });

                itemsTotal += quantityToGive * item.UnitCost;
            }

            return itemsTotal + paidToUserTotal + vendorPaidTotal;
        }

        /// <summary>
        /// Handles the monetary path: checks available funds, deducts from the
        /// location's monetary inventory, and records a MonetaryDisbursementTransaction.
        /// Returns the total amount disbursed.
        /// </summary>
        private async Task<decimal> ProcessMonetaryDisbursementAsync(
            InputPackageSubscription subscription,
            OrganizationLocation agentLocation,
            InputSubscriptionDisbursement approvedRequest,
            DisburseSubscriptionRequest request,
            InputSubscriptionRequestArgs args,
            string reference,
            bool isPartial)
        {
            decimal subscribedAmount = subscription.TotalFinancingValue;

            decimal availableFunds = await _inputFinancingRepository
                .GetAvailableBalanceAsync(agentLocation.Id);

            if (availableFunds <= 0)
                throw new DisbursementException(
                    $"No funds available at location '{agentLocation.LocationName}'.");

            decimal amountToDisburse = Math.Min(subscribedAmount, availableFunds);
            decimal amountPending = subscribedAmount - amountToDisburse;
            if (amountPending > 0) isPartial = true;

            await _inputFinancingRepository.DeductAsync(
                agentLocation.Id, amountToDisburse, reference, args.UserId);

            _context.MonetaryDisbursementTransactions.Add(new MonetaryDisbursementTransaction
            {
                DisbursementId = approvedRequest.Id,
                SubscriptionId = subscription.Id,
                LocationId = agentLocation.Id,
                SubscribedAmount = subscribedAmount,
                DisbursedAmount = amountToDisburse,
                PendingAmount = amountPending,
                DisbursedOn = DateTimeOffset.UtcNow,
                AgentUserId = args.UserId,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                ReferenceNumber = reference,
            });

            return amountToDisburse;
        }
        #endregion

        #region reporting

        #endregion
    }
}
