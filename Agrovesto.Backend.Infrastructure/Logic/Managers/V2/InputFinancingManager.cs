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
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService.Model;
using Core.Models.Payment;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using PhoneNumbers;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Org.BouncyCastle.Math.EC.ECCurve;
using SubscriptionDocument = Agrovesto.Backend.Infrastructure.Entities.Businesses.SubscriptionDocument;

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
        Task<bool> ApproveInputDisbursementRequestAsync(ApproveDisbursementRequestDto request, InputSubscriptionRequestArgs args);
        Task<DisburseSubscriptionResult> DisburseSubscriptionAsync(DisburseSubscriptionRequest request, SubscriptionQueryFilter args);
        Task<long> RequestInputDisbursementApprovalAsync(InputSubscriptionRequestArgs args);
        Task<CommitmentFormDto> DownloadCommitmentLetter(SubscriptionQueryFilter filter);
        Task<(byte[] FileBytes, string FileName)> UploadCommissionLetterAsync(long subscriptionId, string userId, IFormFile file);
        Task<PaymentReceiptResponse> GetPaymentReceiptAsync(long subscriptionId, string currentloggedInrole);
        Task<List<Core.Models.Businesses.Input.SubscriptionDocument>> GetDocuments(long subscriptionId);
        Task SendRepaymentReminderAsync(SubscriptionQueryFilter filter);
        Task<RepaymentResponse> RecordRepaymentAsync(RecordRepaymentRequest request, InputSubscriptionRequestArgs args);
        Task<List<RepaymentScheduleItemDto>> GetRepaymentScheduleAsync(SubscriptionQueryFilter filter);
        Task<PagedResult<RepaymentLedgerEntryDto>> GetRepaymentLedgerAsync(SubscriptionQueryFilter filter);
        Task<FarmerSubscriptionOverviewDto> GetAgentSubscriptionOverview(SubscriptionQueryFilter filter);
        Task<bool> RemoveSubscriptionAsync(InputSubscriptionRequestArgs query, CancellationToken cancellationToken = default);
        Task<EditRepaymentPlanResult> EditRepaymentPlanAsync(long packageId, UpdateRepaymentPlanRequest request, InputPackageConfigArgs args);
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

        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public InputFinancingManager(IWalletServiceFactory factory, IWalletProviderRepository providerRepo, IUserWalletRepository userWalletRepository,
            IInputFinancingRepository inputFinancingRepository, IBusinessRepository businessRepository,
            IWalletProviderRepository walletProviderRepository, IOnBoardingManager onBoardingManager, IAWSService aWSService,
            IConfiguration configuration, INotificationService notificationService,
            DataContext context, IMTNMomo mtnMomoService,
            IAWSRekognitionManager imageRecognitionService,
            IUserWalletRepository walletTransactionRepository,
            IFarmerRepository farmerRepository
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

            if (request.PaybackWalletId.HasValue && request.PaybackWalletId.Value <= 0)
            {
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Invalid Payback Wallet Selected");

            }
            if (request.CommitmentRequired &&
  (!request.PaybackWalletId.HasValue ||
   request.PaybackWalletId.Value == 0))
            {
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    "A wallet provider must be selected when commitment is required."
                );
            }

            if (!request.CommitmentRequired &&
   !request.RepaymentRequired &&
   request.PaybackWalletId.HasValue)
            {
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    "A payback wallet cannot be selected when neither commitment nor repayment is required. Please remove the payback wallet or enable commitment or repayment."
                );
            }
            if (request.RepaymentRequired &&
(!request.PaybackWalletId.HasValue ||
request.PaybackWalletId.Value == 0))
            {
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    "A wallet provider must be selected when repayment is required."
                );
            }
            if (!request.PaybackWalletId.HasValue)
            {
                return await _inputFinancingRepository.CreateInputFinancingPackageConfig(request, args);
            }
          
            var provider = await _walletproviderRepo.GetById(request.PaybackWalletId.Value);

            if (provider == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Wallet Provider not found");

            var userWallet = await _userWalletRepository.GetUserWalletByProviderId(new UserWalletParams
            {
                Id = request.PaybackWalletId.Value,
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
                    throw new BaseException(
                        ExceptionType.OPERATION_FAILED,
                        "The repayment wallet provider has not been configured for this package. Please contact support."
                    );

                var userWallet = await _userWalletRepository.GetUserWalletByProviderId(new UserWalletParams
                {
                    Id = package.WalletProviderId,
                    UserId = query.UserId,
                    businessCode = query.businessCode
                });

                if (userWallet == null)
                    throw new BaseException(
                        ExceptionType.OPERATION_FAILED,
                        "No eligible wallet was found for repayment configured for this package. Please ensure the package has a wallet with the required provider or contact support."
                    );

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

            if (request.PaybackWalletId.HasValue)
            {
                var provider = await _walletproviderRepo.GetById(request.PaybackWalletId.Value);

                if (provider == null)
                    throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Wallet Provider not found");

                var userWallet = await _userWalletRepository.GetUserWalletByProviderId(new UserWalletParams
                {
                    Id = request.PaybackWalletId.Value,
                    UserId = args.createdByUserId,
                    businessCode = args.BusinessCode
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
            }

            return await _inputFinancingRepository.UpdateInputPackageConfig(packageId, request, args);
        }
        public async Task<EditRepaymentPlanResult> EditRepaymentPlanAsync(
        long packageId,
        UpdateRepaymentPlanRequest request,
        InputPackageConfigArgs args)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            if (packageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(packageId));

            return await _inputFinancingRepository.EditRepaymentPlanAsync(packageId, request, args);
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
        public async Task<bool> RemoveSubscriptionAsync(InputSubscriptionRequestArgs query,
    CancellationToken cancellationToken = default)
        {
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));

            if (query.SubscriptionId <= 0) throw new ArgumentException("Invalid Id provided", nameof(query.SubscriptionId));

            return await _inputFinancingRepository.RemoveSubscriptionAsync(query, cancellationToken);
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
        public async Task<FarmerSubscriptionOverviewDto> GetAgentSubscriptionOverview(
            SubscriptionQueryFilter filter)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(filter.OrganizationCode);
            if (hasOrg != null)
            {
                filter.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.GetAgentSubscriptionOverview(filter);
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
        public async Task<CommitmentFormDto> DownloadCommitmentLetter(SubscriptionQueryFilter filter)
        {
            if (!filter.ViewType.HasValue)
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "View type is required");
            }
            if (!Enum.IsDefined(typeof(SubscriptionView), filter.ViewType))
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid Subscription view type");
            }

            var subscriptiondetail = await _inputFinancingRepository.GetSubscriptionDetail(filter);

            var data = new CommitmentFormData
            {
                FullName = subscriptiondetail.Subscriber.Name,
                SubscriptionId = subscriptiondetail.SubscriptionId,
                SubscriberType = subscriptiondetail.Subscriber.Type,
                PhoneNumber = subscriptiondetail.Subscriber.PhoneNumber,
                State = subscriptiondetail.Subscriber.State,
                LGA = subscriptiondetail.Subscriber.Lga,
                PackageName = subscriptiondetail.Package.PackageName,
                Crop = subscriptiondetail.Package.PackageTarget,
                GrowthCycleSeason = subscriptiondetail.Package.Season,
                PackageInputValue = subscriptiondetail.Package.TotalValue,
                CommitmentAmount = subscriptiondetail.Package.CommitmentValue,
                RepaymentDuration = subscriptiondetail.Package.RepaymentDuration,
                CommitmentPlan = subscriptiondetail.Package.CommitmentPlan,
                StartDate = subscriptiondetail.Package.RepaymentStartDate,
                EndDate = subscriptiondetail.Package.RepaymentEndDate,
                CommitmentAmountInWords = NumberToWords.Convert(subscriptiondetail.Package.CommitmentValue)
            };

            var pdfBytes = CommitmentFormGenerator.Generate(data);

            return new CommitmentFormDto
            {
                FileName = $"CommitmentForm_{data.SubscriptionId}.pdf",
                ContentType = "application/pdf",
                FileContent = pdfBytes
            };
        }
        public async Task<(byte[] FileBytes, string FileName)> UploadCommissionLetterAsync(long subscriptionId, string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new BaseException(ExceptionType.BAD_REQUEST, "No file was uploaded");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Only PDF, JPG, or PNG files are allowed");

            if (file.Length > MaxFileSizeBytes)
                throw new BaseException(ExceptionType.BAD_REQUEST, "File size must not exceed 10MB");

            var subscriptionExists = await _context.InputPackageSubscriptions.AnyAsync(x => x.Id == subscriptionId);
            if (!subscriptionExists)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Subscription not found");

            var s3Key = $"commitment-letters/{subscriptionId}/{Guid.NewGuid():N}{extension}";

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            await _imageRecognitionService.UploadFileAsync(s3Key, ms, file.ContentType);

            var existingDocument = await _context.SubscriptionDocuments
                .FirstOrDefaultAsync(x =>
                    x.SubscriptionId == subscriptionId &&
                    x.DocumentType == Core.Models.Businesses.Input.DocumentType.CommissionLetter);

            if (existingDocument != null)
            {
                if (!string.IsNullOrWhiteSpace(existingDocument.Key))
                {
                    await _imageRecognitionService.DeleteFileAsync(existingDocument.Key);
                }

                existingDocument.Key = s3Key;
                existingDocument.FileName = file.FileName;
                existingDocument.UploadedOn = DateTimeOffset.UtcNow;
                _context.SubscriptionDocuments.Update(existingDocument);
            }
            else
            {
                _context.SubscriptionDocuments.Add(new SubscriptionDocument
                {
                    SubscriptionId = subscriptionId,
                    DocumentType = Core.Models.Businesses.Input.DocumentType.CommissionLetter,
                    Key = s3Key,
                    FileName = file.FileName,
                    UploadedOn = DateTimeOffset.UtcNow,
                    UploadedBy = userId,
                    FilePath = s3Key

                });
            }

            await _context.SaveChangesAsync();

            return (ms.ToArray(), file.FileName);
        }

        public async Task<List<Core.Models.Businesses.Input.SubscriptionDocument>> GetDocuments(long subscriptionId)
        {
            var document = await _context.SubscriptionDocuments
                 .Where(x =>
                     x.SubscriptionId == subscriptionId).ToListAsync();

            if (document.Count == 0)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Commission letter not found for this subscription");

            List<Core.Models.Businesses.Input.SubscriptionDocument> response = document.Select(x => new Core.Models.Businesses.Input.SubscriptionDocument
            {
                FileName = x.FileName,
                FilePath = x.FilePath,
                Id = x.Id,
                Key = x.Key,
                SubscriptionId = x.SubscriptionId,
                Type = x.DocumentType,
                UploadedOn = x.UploadedOn
            }).ToList();



            return response;
        }
        public async Task<(byte[] FileBytes, string FileName)> DownloadCommissionLetterAsync(long subscriptionId)
        {
            var document = await _context.SubscriptionDocuments
                .FirstOrDefaultAsync(x =>
                    x.SubscriptionId == subscriptionId &&
                    x.DocumentType == Core.Models.Businesses.Input.DocumentType.CommissionLetter);

            if (document == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Commission letter not found for this subscription");

            if (string.IsNullOrWhiteSpace(document.Key))
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Invalid document storage key");

            var fileBytes = await _imageRecognitionService.DownloadFileAsync(document.Key);

            var fileName = string.IsNullOrWhiteSpace(document.FileName)
                ? $"commission_letter_{subscriptionId}.pdf"
                : document.FileName;

            return (fileBytes, fileName);
        }

        public async Task<PaymentReceiptResponse> GetPaymentReceiptAsync(long subscriptionId, string currentloggedInrole)
        {
            var agentroute = _configuration["Receipt:AgentRoute"];
            var orgRoute = _configuration["Receipt:OrgRoute"];

            var baseUrl = _configuration["Receipt:BaseUrl"];
            if (currentloggedInrole == UserType.Agent.ToString())
            {
                baseUrl += $"/{agentroute}";
            }
            else
            {
                baseUrl += $"/{orgRoute}";
            }
            // Fetch the subscription with all successful commitment payments
            var subscription = await _context.InputPackageSubscriptions
                .Include(x => x.InputPackage)
                    .ThenInclude(x => x.Organization)
                .Include(x => x.InputPackage)
                    .ThenInclude(x => x.RepaymentPlan)
                .Include(x => x.SubscriberUser)
                .Include(x => x.Payments.Where(p =>
                    p.PaymentType == PaymentType.Commitment &&
                    p.Status == TransactionStatus.Successful))
                    .ThenInclude(x => x.RecordedByUser)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Subscription not found");

            var package = subscription.InputPackage;

            var commitmentPayments = subscription.Payments
                .Where(p =>
                    p.PaymentType == PaymentType.Commitment &&
                    p.Status == TransactionStatus.Successful)
                .OrderBy(p => p.PaidOn)
                .ToList();

            if (!commitmentPayments.Any())
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "No successful commitment payments found for this subscription");

            var totalAmountPaid = commitmentPayments.Sum(p => p.AmountPaid);

            var latestPayment = commitmentPayments.Last();

            var agent = latestPayment.RecordedByUser;

            //var totalInstallments = commitmentPayments.Count;
            var totalInstallments = CalculateTotalInstallments(
        package.RepaymentPlan.RepaymentStartDate!.Value,
        package.RepaymentPlan.RepaymentEndDate!.Value,
        subscription.RepaymentFrequency);

            var installmentAmount = package.RepaymentPlan.RepaymentValue / totalInstallments;
            var receiptNumber = $"RCP-{package.InputSeason}-{package.InputTarget}-{latestPayment.PaidOn:yyyyMMdd}-{subscriptionId:D4}";

            var data = new PaymentReceiptData
            {
                ReceiptNumber = receiptNumber,
                PaymentDate = latestPayment.PaidOn ?? DateTimeOffset.UtcNow,

                // Subscriber
                FullName = subscription.SubscriberUser.Name,
                SubscriberId = subscription.SubscriberUser.Id,
                SubscriberType = EnumExtensions.GetDisplayName(subscription.SubscriberType),

                // Package
                PackageName = package.Name,
                GrowthCycle = package.InputSeason,
                RepaymentPlan = EnumExtensions.GetDisplayName(subscription.RepaymentFrequency),
                //InstallmentLabel = $"{totalInstallments} Payment{(totalInstallments > 1 ? "s" : "")}",

                InstallmentLabel = $"{totalInstallments} installment{(totalInstallments > 1 ? "s" : "")} of ₦{installmentAmount:N2}",
                PeriodStart = package.RepaymentPlan?.RepaymentStartDate?.ToString("MMM yyyy"),
                PeriodEnd = package.RepaymentPlan?.RepaymentEndDate?.ToString("MMM yyyy"),

                // Payment — reflects the most recent transaction
                PaymentChannel = latestPayment.PaymentChannel.ToString(),
                PaymentReference = latestPayment.ExternalReference,
                CollectedByName = agent?.Name ?? "-",
                CollectedByCode = agent?.Id ?? "-",

                // Amounts — summed across all commitment payments
                AmountPaid = totalAmountPaid,
                RepaymentValue = package.RepaymentPlan.RepaymentValue,
                TotalPackageValue = package.TotalFinancingValue,
                AmountInWords = NumberToWords.Convert(totalAmountPaid),

                // QR
                SubscriptionId = subscription.Id,
                QrUrl = ReceiptQrUrlBuilder.Build(
                    subscriptionId: subscription.Id,
                    paymentId: 0,           // no longer payment-specific
                    baseUrl: $"{baseUrl}/subscriber/{subscription.Id}?viewType=3"!,
                    hmacSecret: _configuration["Receipt:HmacSecret"]!)
            };

            var pdfBytes = PaymentReceiptGenerator.Generate(data);

            return new PaymentReceiptResponse
            {
                FileName = $"{data.ReceiptNumber}.pdf",
                ContentType = "application/pdf",
                FileContent = pdfBytes
            };
        }

        public static int CalculateTotalInstallments(
DateTimeOffset startDate,
DateTimeOffset endDate,
RepaymentFrequency frequency)
        {
            if (endDate < startDate)
                throw new ArgumentException("End date cannot be before start date.");

            return frequency switch
            {
                RepaymentFrequency.Daily =>
                    (int)Math.Floor((endDate - startDate).TotalDays / 7) + 1,

                RepaymentFrequency.Weekly =>
                    (int)Math.Floor((endDate - startDate).TotalDays / 14) + 1,

                RepaymentFrequency.Monthly =>
                    ((endDate.Year - startDate.Year) * 12)
                    + endDate.Month - startDate.Month + 1,

                RepaymentFrequency.Quarterly =>
                    (((endDate.Year - startDate.Year) * 12)
                    + endDate.Month - startDate.Month) / 3 + 1,

                RepaymentFrequency.Yearly =>
                    endDate.Year - startDate.Year + 1,

                _ => 1
            };
        }

        public async Task<bool> CommitmentPaymentAsync(
           RecordCommitmentPaymentRequest request,
           InputSubscriptionRequestArgs args)
        {
            ValidateRequest(request);

            request.SubscriptionId = args.SubscriptionId;
            request.SubscriberUserId = args.SubscriberUserId;
            ValidateArgs(request);

            var subscription = await GetSubscriptionAsync(request, args);
            if(subscription.InputPackage.CommitmentRequired && (!subscription.InputPackage.WalletProviderId.HasValue || subscription.InputPackage.WalletProviderId == null || subscription.InputPackage.WalletProviderId == 0))
            {
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    "No wallet provider was configured for payment for this package.");
            }
          
            if (subscription.InputPackage.WalletProviderId == null || !subscription.InputPackage.WalletProviderId.HasValue)
            {
                throw new BaseException(ExceptionType.OPERATION_FAILED, "This package has no wallet configuration. Please contact support.");
            }
            var configredProvider = await _providerRepo.GetById(subscription.InputPackage.WalletProviderId.Value);

            var requestProvider = await _providerRepo.GetById(request.WalletProviderId);

            ValidateSubscription(subscription, request, configredProvider, requestProvider);

            var amountToPay = await CalculateAmountToPayAsync(request, subscription);

            await ProcessPaymentAsync(request, args, subscription, amountToPay);

            return true;
        }

        private static void ValidateRequest(RecordCommitmentPaymentRequest request)
        {
            if (request == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Invalid request");
        }

        private static void ValidateArgs(RecordCommitmentPaymentRequest request)
        {
            if (request.SubscriptionId <= 0)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Subscription Id required");

            if (string.IsNullOrEmpty(request.SubscriberUserId))
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Subscriber User Id required");
        }

        private async Task<InputPackageSubscription> GetSubscriptionAsync(
            RecordCommitmentPaymentRequest request,
            InputSubscriptionRequestArgs args)
        {
            return await _context.InputPackageSubscriptions
                .Include(x => x.InputPackage)
                    .ThenInclude(x => x.Organization)
                .Include(x => x.InputPackage)
                    .ThenInclude(x => x.CommitmentPlan)
                .Include(x => x.SubscriberUser)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SubscriptionId &&
                    x.SubscriberUserId == args.SubscriberUserId);
        }

        private static async Task ValidateSubscription(
            InputPackageSubscription subscription,
            RecordCommitmentPaymentRequest request, WalletProviderModel configuredProvider, WalletProviderModel requestProvider)
        {
            
            if(configuredProvider == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Configured package wallet provider not found");
            if(requestProvider == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Request wallet provider not found");

            if (subscription == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Subscription not found");

            if (subscription.InputPackage == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Cannot find input package");

            if (!subscription.InputPackage.CommitmentRequired)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Commitment not required");


            if (subscription.InputPackage.CommitmentPlan == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Commitment plan not found for this input package");

            if (subscription.InputPackage.CommitmentPlan.CommitmentPaymentEndDate < DateTimeOffset.UtcNow)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Commitment Payment cut off window has expired, you can no longer make commitmen fee, reach out to your admin");


            if (subscription.Status == SubscriptionStatus.PendingApproval)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Can only pay commitment fee after subscription is approved");

            if (subscription.Status == SubscriptionStatus.PendingDisbursement)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Already completed commitment stage");


            if (subscription.Status != SubscriptionStatus.PendingCommitment)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Current state of subscription does not indicate pending commitment");

            

            if (subscription.InputPackage.WalletProviderId != request.WalletProviderId)
                throw new BaseException(ExceptionType.OPERATION_FAILED, 
                    $"Selected wallet {requestProvider.Name} does not match configured package wallet {configuredProvider.Name}, please use the configured package wallet");

            if (subscription.SubscriberUser == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Subscriber not found. Please try again later.");

            if (subscription.InputPackage.Organization == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Organization not found for this input package.");
        }

        private async Task<decimal> CalculateAmountToPayAsync(
            RecordCommitmentPaymentRequest request,
            InputPackageSubscription subscription)
        {
            //if(subscription.InputPackage.CommitmentPlan != null && subscription.InputPackage.CommitmentPlan.CommitmentType == CommitmentType.PercentageOfTotalValue)
            //{
            //    subscription.CommitmentValue = subscription.CommitmentValue * subscription.TotalFinancingValue;
            //}
            var outstanding = subscription.CommitmentValue -
                await _inputFinancingRepository.GetTotalCommitmentPaidAsync(subscription.Id);

            if (outstanding <= 0)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Already fully paid");

            var amountToPay = request.Amount > 0 ? request.Amount : outstanding;
            amountToPay = Math.Round(amountToPay, 2);

            if (amountToPay > outstanding)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Amount exceeds outstanding balance");

            return amountToPay;
        }

        private async Task ProcessPaymentAsync(
            RecordCommitmentPaymentRequest request,
            InputSubscriptionRequestArgs args,
            InputPackageSubscription subscription,
            decimal amountToPay)
        {
            switch (request.PaymentChannel)
            {
                case PaymentChannel.Wallet:
                    await ProcessWalletTransferAsync(
                        request,
                        args,
                        subscription,
                        amountToPay,
                        "Payment for Commitment on input subscription",
                        PaymentType.Commitment);
                    break;

                // case PaymentChannel.OffilineBankTransfer:
                //     await _inputFinancingRepository.FlagPendingBankTransferAsync(paymentId);
                //     break;

                // case PaymentChannel.PayemntGateway:
                //     await _inputFinancingRepository.FlagPendingCashApprovalAsync(paymentId);
                //     break;

                default:
                    throw new BaseException(ExceptionType.OPERATION_FAILED, "Channel Not supported");
            }
        }
        private async Task<WalletProviderParameterModel[]> RetrieveParams(string WalletProviderCode, string Env, string businessCode)
        {
            var provider = await _providerRepo.GetProvidersForBusiness(businessCode);
            var providerId = provider.Where(x => x.WalletProvider.Code == WalletProviderCode).FirstOrDefault().ProviderId;
            WalletProviderParameterModel[] parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            return parameters;

        }
        private async Task<WalletTransferResponse> ProcessWalletTransferAsync(
    RecordCommitmentPaymentRequest request,
    InputSubscriptionRequestArgs args,
    InputPackageSubscription subscription,
    decimal amountToPay,
    string description,
    PaymentType paymentType)
        {
            // ─── Step 1: Validate wallets ───
            UserWalletDetailResponseModel debitFromUserWallet = null;
            if (request.PaidFromAccount == PaidBy.CurrentUser)
            {
                debitFromUserWallet = await _userWalletRepository.GetUserWalletByProviderId(
                new UserWalletParams
                {
                    Id = request.WalletProviderId,
                    UserId = args.UserId,
                    businessCode = args.BusinessCode
                });
                if (debitFromUserWallet == null)
                    throw new BaseException(ExceptionType.NOTFOUND, "Current user wallet not found");
            }
            else if (request.PaidFromAccount == PaidBy.Subscriber)
            {

                debitFromUserWallet = await _userWalletRepository.GetUserWalletByProviderId(
                    new UserWalletParams
                    {
                        Id = request.WalletProviderId,
                        UserId = request.SubscriberUserId,
                        businessCode = args.BusinessCode
                    });

                if (debitFromUserWallet == null)
                    throw new BaseException(ExceptionType.NOTFOUND, "Subscriber wallet not found");
            }
            else
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "Could not determine debit account");
            }

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

            if (debitFromUserWallet.WalletBalance < amountToPay)
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

            var provider = await _providerRepo.GetById(debitFromUserWallet.WalletProviderId);

            if (provider == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Wallet provider not found");

            var providerParams = await RetrieveParams(provider.Code, args.Env, args.BusinessCode);
            var providerService = _factory.GetService(provider.Code);

            if (providerService == null)
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    $"Wallet service for {provider.Code} was not found");

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
                            PaymentType = paymentType,
                            PaymentChannel = request.PaymentChannel,
                            Reference = masterReference,
                            WalletProviderId = request.WalletProviderId,
                            RecordedByUserId = args.UserId
                        });

                    await _walletTransactionRepository.InitiateWalletWithdrawal(
                        debitFromUserWallet.Id, amountToPay, description, $"{debitReference}-DB");

                    string? debitProviderReference = null;
                    string providerMessage = "";

                    try
                    {
                        var debitRequest = new WalletTopUpProviderRequest
                        {
                            WalletNumber = debitFromUserWallet.WalletNumber,
                            Amount = amountToPay,
                            Currency = debitFromUserWallet.Currency,
                            Message = description,
                            TransactionReference = debitReference
                        };


                        ProviderDebitResult debitResult = provider.Code switch
                        {
                            "Momo" => await ProcessMomoWalletDebit(
                                          args, debitReference, providerParams, providerService, debitRequest),

                            "Etransact" => await ProcessEtransactWalletDebit(
                                          args, debitReference, providerParams, providerService, debitRequest),

                            _ => throw new BaseException(
                                     ExceptionType.OPERATION_FAILED,
                                     $"Debit not implemented for provider {provider.Code}")
                        };
                        debitProviderReference = debitResult.ProviderReference;
                        providerMessage = debitResult.Message;


                        await _walletTransactionRepository.CompleteWalletWithdrawal(
                            $"{debitReference}-DB", true, debitProviderReference, providerMessage);

                        await _userWalletRepository.AdjustWalletBalanceAsync(
                            debitFromUserWallet.Id, -amountToPay);
                    }
                    catch (BaseException)
                    {
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

                    await _walletTransactionRepository.CreateWalletFundinAsync(
                        organizationWallet.Id, amountToPay, description, $"{creditReference}-CR");

                    try
                    {
                        var transferRequest = new TransferRequest
                        {
                            AccountNumber = organizationWallet.WalletNumber,
                            Amount = $"{amountToPay}",
                            Currency = organizationWallet.Currency,
                            Message = description,
                            Reference = creditReference,
                            SenderName = subscription.SubscriberUser.Name,
                            SenderAccountNumber = debitFromUserWallet.WalletNumber,
                            RecipientName = subscription.InputPackage.Organization.CompanyName
                                ?? subscription.InputPackage.Organization.FullName
                        };

                        var transferResponse = await providerService.InitiateWalletTransferAsync(
                            transferRequest, providerParams, args.Env);

                        if (transferResponse == null)
                        {
                            await _walletTransactionRepository.CompleteWalletFunding(
                                $"{creditReference}-CR", false, null, "Wallet transfer returned no response");

                            await dbTransaction.CommitAsync();

                            await _walletTransactionRepository.FlagForReconciliation(
                                $"{debitReference}-DB", $"{creditReference}-CR",
                                "Credit returned no response after successful debit");

                            throw new BaseException(
                                ExceptionType.OPERATION_FAILED,
                                "Wallet service did not return a response. Our team has been notified to reconcile your transaction.");
                        }

                        switch (provider.Code)
                        {
                            case "Momo":
                                {
                                    ProcessMomoWalletTransfer(args, debitReference, creditReference, providerParams, dbTransaction);

                                    break;
                                }

                            case "Etransact":
                                //credit on etransact
                                break;

                            default:
                                await dbTransaction.RollbackAsync();

                                throw new BaseException(
                                    ExceptionType.OPERATION_FAILED,
                                    $"Credit not implemented for provider {provider.Code}");
                        }

                        await _walletTransactionRepository.CompleteWalletFunding(
                            $"{creditReference}-CR", true,
                            transferResponse.Data.Reference,
                            transferResponse.Data.Status);

                        await _userWalletRepository.AdjustWalletBalanceAsync(
                            organizationWallet.Id, amountToPay);

                        await _inputFinancingRepository
                            .RecordSuccessfulPaymentAsync(paymentId, new PaymentConfirmationRequest
                            {
                                ExternalReference = masterReference,
                                Status = TransactionStatus.Successful,

                            }, args.UserId);

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
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await _walletTransactionRepository.CompleteWalletFunding(
                            creditReference, false, null, ex.Message);

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

        private async void ProcessMomoWalletTransfer(InputSubscriptionRequestArgs args, string debitReference, string creditReference, WalletProviderParameterModel[] providerParams, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction dbTransaction)
        {
            var transferStatus = await _mtnMomoService.GetTransferStatus(
                creditReference, providerParams, args.Env);

            if (transferStatus == null || !transferStatus.IsSuccess)
            {
                var creditFailMessage = transferStatus?.Message ?? "Wallet transfer failed";

                await _walletTransactionRepository.CompleteWalletFunding(
                    $"{creditReference}-CR", false, null, creditFailMessage);

                await dbTransaction.CommitAsync();

                await _walletTransactionRepository.FlagForReconciliation(
                    $"{debitReference}-DB", $"{creditReference}-CR",
                    $"Credit failed after successful debit: {creditFailMessage}");

                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "Payment was debited but could not be credited to the organization. Our team has been notified to reconcile your transaction.");
            }
        }

        private async Task<ProviderDebitResult> ProcessMomoWalletDebit(InputSubscriptionRequestArgs args, string debitReference, WalletProviderParameterModel[] providerParams,
            IGenericWalletService providerService, WalletTopUpProviderRequest debitRequest)
        {
            var result = new ProviderDebitResult();
            var debitResponse = await providerService.DebitWalletAsync(
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

            result.ProviderReference = debitResponse.Data.Reference;
            result.Message = debitResponse.Status;

            return result;
        }
        private async Task<ProviderDebitResult> ProcessEtransactWalletDebit(InputSubscriptionRequestArgs args, string debitReference, WalletProviderParameterModel[] providerParams,
            IGenericWalletService providerService, WalletTopUpProviderRequest debitRequest)
        {
            var result = new ProviderDebitResult();

            var balanceEnquiry = await providerService.BalanceEnquiryAsync(new UserProfileResponse
            {
                WalletNumber = debitRequest.WalletNumber,
            }, providerParams, args.Env);

            if (balanceEnquiry.IsSuccess &&
                balanceEnquiry.Data != null &&
                balanceEnquiry.Data.balance > 0)
            {
                var walletBalance = balanceEnquiry.Data.balance;

                if (walletBalance < debitRequest.Amount)
                {
                    await _walletTransactionRepository.CompleteWalletWithdrawal(
                        $"{debitReference}-DB", false, null, "Insufficient wallet balance");
                    throw new BaseException(
                        ExceptionType.OPERATION_FAILED,
                        $"Insufficient wallet balance. Kindly top up at least ₦{debitRequest.Amount:N0}");
                }
            }
            else
            {
                await _walletTransactionRepository.CompleteWalletWithdrawal(
                    $"{debitReference}-DB", false, null, "Could not verify wallet balance");
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "Could not verify wallet balance. Please try again later.");
            }

            var debitResponse = await providerService.DebitWalletAsync(
                debitRequest, providerParams, args.Env);

            if (debitResponse == null)
            {
                await _walletTransactionRepository.CompleteWalletWithdrawal(
                    $"{debitReference}-DB", false, null, "Wallet debit request returned no response");

                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "Could not initiate wallet debit");
            }
            result.ProviderReference = debitResponse.Data.ProviderReference;
            result.Message = debitResponse.Status;
            return result;
        }

        public async Task<RepaymentResponse> RecordRepaymentAsync(
    RecordRepaymentRequest request,
    InputSubscriptionRequestArgs args)
        {
            if (request == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Invalid request");

            request.SubscriptionId = args.SubscriptionId;
            request.SubscriberUserId = args.SubscriberUserId;

            if (request.SubscriptionId <= 0) throw new BaseException(ExceptionType.OPERATION_FAILED, "Subscription Id required");
            if (string.IsNullOrEmpty(request.SubscriberUserId)) throw new BaseException(ExceptionType.OPERATION_FAILED, "Subscriber User Id required");

            var subscription = await _context.InputPackageSubscriptions
                .Include(x => x.InputPackage)
                .ThenInclude(x => x.Organization)
                .Include(x => x.SubscriberUser)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SubscriptionId &&
                    x.SubscriberUserId == args.SubscriberUserId);

            if (subscription == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Subscription not found");

            if (subscription.InputPackage == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Cannot find input package");
            var package = subscription.InputPackage;
            if (package != null && !package.RepaymentRequired)
            {
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "This package does not require repayment. Please contact your organization to complete and close your subscription cycle."
                );
            }

            var totalRepaid = await _inputFinancingRepository.GetTotalRepaymentPaidAsync(subscription.Id);
            var outstanding = subscription.RepaymentValue - totalRepaid;

            if (outstanding <= 0)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Repayment already fully completed");



            var reference = !string.IsNullOrWhiteSpace(request.Reference)
                          ? request.Reference
                          : $"{Guid.NewGuid():D}";
            if (!subscription.InputPackage.RepaymentRequired)
            {
                var paymentId = await _inputFinancingRepository.CreatePendingPaymentAsync(
                   new CreateCommitmentPaymentRequest
                   {
                       SubscriptionId = subscription.Id,
                       Amount = 0,
                       PaymentType = PaymentType.Repayment,
                       PaymentChannel = PaymentChannel.Wallet,
                       Reference = reference,
                       ExternalReference = reference,
                       WalletProviderId = request.WalletProviderId.Value,
                       RecordedByUserId = args.UserId
                   });

                await _inputFinancingRepository.RecordSuccessfulPaymentAsync(
                    paymentId,
                    new PaymentConfirmationRequest
                    {
                        ExternalReference = reference,
                        Status = TransactionStatus.Successful
                    }, args.UserId);

                return new RepaymentResponse
                {
                    IsSuccessful = true,
                    AmountApplied = 0,
                    OutstandingBalance = 0,
                    OverpaymentCredited = 0,
                    ExternalReference = reference
                };

            }

            // ─── Outstanding must aggregate across BOTH cash and produce repayments already made ───

            switch (request.PaymentChannel)
            {
                case PaymentChannel.Wallet:
                    {
                        var amountToPay = request.Amount > 0 ? request.Amount : outstanding;
                        amountToPay = Math.Round(amountToPay, 2);

                        if (amountToPay > outstanding)
                            throw new BaseException(ExceptionType.OPERATION_FAILED, "Amount exceeds outstanding repayment balance");

                        var walletRequest = new RecordCommitmentPaymentRequest
                        {
                            SubscriptionId = subscription.Id,
                            SubscriberUserId = args.SubscriberUserId,
                            WalletProviderId = request.WalletProviderId ?? throw new Exception("Wallet provider is required for wallet repayment"),
                            PaymentChannel = request.PaymentChannel,
                            Reference = reference
                        };

                        var walletResponse = await ProcessWalletTransferAsync(
                            walletRequest, args, subscription, amountToPay,
                            "Repayment on input subscription",
                            PaymentType.Repayment);

                        return new RepaymentResponse
                        {
                            IsSuccessful = walletResponse.IsSuccessful,
                            AmountApplied = amountToPay,
                            OutstandingBalance = outstanding - amountToPay,
                            OverpaymentCredited = 0m,
                            ExternalReference = walletResponse.ExternalReference
                        };
                    }
                case PaymentChannel.Produce:
                    {
                        if (request.ProduceDetails == null)
                            throw new BaseException(ExceptionType.OPERATION_FAILED, "Produce details are required for produce repayment");

                        var produceResult = await ProcessProduceRepaymentAsync(
                            subscription, request.ProduceDetails, args, outstanding, reference);

                        return new RepaymentResponse
                        {
                            IsSuccessful = produceResult.IsSuccessful,
                            AmountApplied = produceResult.AppliedAmount,
                            OutstandingBalance = outstanding - produceResult.AppliedAmount,
                            OverpaymentCredited = produceResult.OverpaymentAmount,
                            ExternalReference = produceResult.Reference
                        };
                    }

                default:
                    throw new BaseException(ExceptionType.OPERATION_FAILED, "Channel not supported");
            }
        }

        private async Task<ProduceRepaymentResult> ProcessProduceRepaymentAsync(
    InputPackageSubscription subscription,
    ProduceRepaymentDetails produceRequest,
    InputSubscriptionRequestArgs args,
    decimal outstandingBalance,
    string reference)
        {
            // ─── Step 1: Validate the product is configured at this location ───

            var inventory = await _context.LocationInventories
                .Include(x => x.OrganizationProduct)
                .FirstOrDefaultAsync(x =>
                    x.OrganizationProductId == produceRequest.OrganizationProductId &&
                    x.LocationId == produceRequest.LocationId &&
                    x.LocationInventoryType == LocationInventoryType.Product &&
                    x.IsActive == true &&
                    x.DeletedOn == null);

            if (inventory == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    $"Product '{produceRequest.OrganizationProductId}' is not configured at the selected location");

            if (produceRequest.QuantityBrought <= 0)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Quantity brought must be greater than zero");

            // ─── Step 2: Price — grade is descriptive only, price comes straight from the product ───

            decimal pricePerUnit = inventory.OrganizationProduct.PricePerUnit;

            if (pricePerUnit <= 0)
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    $"No valid market price configured for '{inventory.OrganizationProduct.ProductName}'");

            decimal equivalentAmount = Math.Round(produceRequest.QuantityBrought * pricePerUnit, 2);

            // ─── Step 3: Split into what applies to the balance vs what becomes a credit ───

            decimal appliedAmount = Math.Min(equivalentAmount, outstandingBalance);
            decimal overpaymentAmount = equivalentAmount - appliedAmount;

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ─── Receive the produce into inventory ───

                    decimal quantityBefore = inventory.Quantity;
                    inventory.Quantity += produceRequest.QuantityBrought;

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        OrganizationProductId = inventory.OrganizationProductId,
                        ToLocationId = produceRequest.LocationId,
                        Quantity = produceRequest.QuantityBrought,
                        QuantityBefore = quantityBefore,
                        QuantityAfter = inventory.Quantity,
                        TransactionType = InventoryTransactionType.RepaymentReceipt,
                        ReferenceNumber = reference,
                        SubscriptionId = subscription.Id,
                        Grade = produceRequest.Grade,
                        UnitPriceUsed = pricePerUnit,
                        TransactionDate = DateTimeOffset.UtcNow,
                        Latitude = produceRequest.Latitude.Value,
                        Longitude = produceRequest.Longitude.Value,
                        PerformedByUserId = args.UserId,
                    });

                    await _context.SaveChangesAsync();

                    // ─── Record the payment against the subscription ───
                    // Produce settles immediately — no external provider call to wait on —
                    // so the payment is created and confirmed inside the same transaction.

                    var paymentId = await _inputFinancingRepository.CreatePendingPaymentAsync(
                        new CreateCommitmentPaymentRequest
                        {
                            SubscriptionId = subscription.Id,
                            Amount = appliedAmount,
                            PaymentType = PaymentType.Repayment,
                            PaymentChannel = PaymentChannel.Produce,
                            Reference = reference,
                            ExternalReference = reference,
                            RecordedByUserId = args.UserId
                        });

                    await _inputFinancingRepository.RecordSuccessfulPaymentAsync(
                        paymentId,
                        new PaymentConfirmationRequest
                        {
                            ExternalReference = reference,
                            Status = TransactionStatus.Successful
                        }, args.UserId);

                    // ─── Overpayment becomes a standing credit balance on the subscription ───

                    if (overpaymentAmount > 0)
                    {
                        await _inputFinancingRepository.AddCreditBalanceAsync(
                            subscription.Id, overpaymentAmount, reference);
                    }

                    await dbTransaction.CommitAsync();

                    return new ProduceRepaymentResult
                    {
                        IsSuccessful = true,
                        EquivalentAmount = equivalentAmount,
                        AppliedAmount = appliedAmount,
                        OverpaymentAmount = overpaymentAmount,
                        Reference = reference
                    };
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task SendCommitmentReminderAsync(SubscriptionQueryFilter filter)
        {

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
                        $"{reminder.Message}", filter.BusinessCode);
            }
            await _awsService.SendEmailAsync(
                   senderEmail,
                   "agrovesto@gmail.com",
                   "Commitment Payment Reminder",
                   $"You've been copied on the details below for Organization Input Financing of '{reminder.Organization.OrganizationName}' '{reminder.Message}'",
                   $"You've been copied on the details below for Organization Input Financing of '{reminder.Organization.OrganizationName}' '{reminder.Message}'"
                   );


        }
        public async Task SendRepaymentReminderAsync(SubscriptionQueryFilter filter)
        {

            var reminder = await _inputFinancingRepository.GetRepaymentReminderAsync(filter.SubscriptionId);

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
                    "Outstanding Repayment Reminder",
                   $"{reminder.Message}",
                   $"{reminder.Message}"
                    );

            }
            else
            {
                await _sMSService.SendSmsAsync(
                        destination,
                        $"{reminder.Message}", filter.BusinessCode);
            }
            await _awsService.SendEmailAsync(
                   senderEmail,
                   "agrovesto@gmail.com",
                   "Outstanding Repayment Reminder",
                   $"You've been copied on the details below for Organization Input Financing of '{reminder.Organization.OrganizationName}' '{reminder.Message}'",
                   $"You've been copied on the details below for Organization Input Financing of '{reminder.Organization.OrganizationName}' '{reminder.Message}'"
                   );


        }
        public async Task<List<RepaymentScheduleItemDto>> GetRepaymentScheduleAsync(SubscriptionQueryFilter filter)
        {
            return await _inputFinancingRepository.GetRepaymentScheduleAsync(filter.SubscriptionId);


        }
        public async Task<PagedResult<RepaymentLedgerEntryDto>> GetRepaymentLedgerAsync(SubscriptionQueryFilter filter)
        {
            return await _inputFinancingRepository.GetRepaymentLedgerAsync(filter.SubscriptionId, filter.PageNumber, filter.PageSize);


        }

        public async Task<InputFinancingDashboardDto> GetInputFinancingDashboardAsync(
         SubscriptionQueryFilter filter)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(filter.OrganizationCode);
            if (hasOrg != null)
            {
                filter.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.GetInputFinancingOverviewAsync(filter);
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
          InputSubscriptionRequestArgs args)
        {
            return await _inputFinancingRepository.ApproveInputDisbursementRequestAsync(request, args);
        }
        public async Task<long> RequestInputDisbursementApprovalAsync(
          InputSubscriptionRequestArgs args)
        {
            var reference = ReferenceUtil.GenerateDisbursementReference();
            return await _inputFinancingRepository.RequestInputDisbursementApprovalAsync(args.SubscriptionId, reference, args.UserId
                , args.SubscriberUserId);
        }
        private async Task<OrganizationAgent> ResolveOrganizationAgentLocation(long agentId, string? organizationCode, long locationId)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new BaseException(ExceptionType.NOTFOUND, "Organization code is required");

            var codes = organizationCode
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();

            var organizations = await _context.Organizations
                .Where(x => codes.Contains(x.OrganizationCode))
                .ToListAsync();

            if (!organizations.Any())
                throw new BaseException(ExceptionType.NOTFOUND, "Organization not found");

            var orgIds = organizations.Select(o => o.OrganizationId).ToList();

            var agentAssignments = await _context.OrganizationAgents
                .Include(x => x.Location)
                .Where(x =>
                    x.AgentId == agentId &&
                    orgIds.Contains(x.OrganizationId) &&
                    x.LocationId == locationId &&
                    x.IsActive)
                .FirstOrDefaultAsync();



            return agentAssignments;
        }

        private async Task<OrganizationLocation> ResolveOrganizationUserLocation(long organizationId, long locationId)
        {
            var orgLocation = await _context.OrganizationLocations
                 .Where(x =>
                     x.OrganizationId == organizationId &&
                     x.Id == locationId &&
                     x.IsActive)
                 .FirstOrDefaultAsync();
            if (orgLocation == null)
            {
                throw new DisbursementException(
                       "Selected location not found in this organization " +
                       "and cannot perform disbursements.");
            }

            return orgLocation;
        }



        public async Task<DisburseSubscriptionResult> DisburseSubscriptionAsync(
            DisburseSubscriptionRequest request,
            SubscriptionQueryFilter args)
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
                    if (string.IsNullOrEmpty(args.OrganizationCode))
                        throw new DisbursementException("Organization disbursement requires valid organizationCode.");

                    if (args.SubscriptionId <= 0)
                        throw new DisbursementException("A valid Subscription Id is required.");


                    if (Math.Abs(request.Latitude) < 1e-6 && Math.Abs(request.Longitude) < 1e-6)
                        throw new DisbursementException(
                            "Geolocation is required for disbursement. " +
                            "Ensure location permissions are granted on your device.");

                    ValidateLiveImage(request.UserImage);
                    var query = await _context.InputSubscriptionDisbursements
                        .Include(x => x.Subscription)
                            .ThenInclude(x => x.InputPackage)
                                .ThenInclude(x => x.InputPackageItems)
                                .ThenInclude(x => x.OrganizationProduct)
                        .Include(x => x.Subscription)
                            .ThenInclude(x => x.InputPackage)
                                .ThenInclude(x => x.AdditionalCosts)
                        .Include(x => x.Subscription)
                            .ThenInclude(x => x.InputPackage)
                                .ThenInclude(x => x.RepaymentPlan)
                        .Include(x => x.Subscription)
                            .ThenInclude(x => x.InputPackage)
                                .ThenInclude(x => x.Organization)
                        .Include(x => x.RequestForUser)
                        .FirstOrDefaultAsync(x => x.SubscriptionId == args.SubscriptionId
                            && x.RequestForUserId == args.SubscriberUserId);

                    if (query == null)
                        throw new DisbursementException("Disbursement Request  not found.");


                    if (query.Subscription.InputPackage.Organization == null)
                        throw new DisbursementException("Organization not found for this input package.");
                    args.OrganizationId = query.Subscription.InputPackage.Organization.OrganizationId;

                    var allowedStatuses = new[] { DisbursementStatus.Approved };
                    if (!allowedStatuses.Contains(query.Status))
                        throw new BaseException(ExceptionType.OPERATION_FAILED, $"" +
                            $"Subscription is not approved " +
                            $"for disbursement or already disbursed. Current subscription status: {query.Status.ToString()}");


                    if (query.Subscription.InputPackage.RepaymentRequired && query.Subscription.InputPackage.RepaymentPlan == null)
                        throw new DisbursementException("Repayment plan is required before disbursement.");

                    var org = query.Subscription.InputPackage.Organization;

                    if (string.IsNullOrEmpty(query.RequestForUserId))
                        throw new BaseException(ExceptionType.OPERATION_FAILED, "Subscriber user ID is missing in the disbursement request.");

                    var subscriberDetails = await _onBoardingManager.GetUseridentity(query.RequestForUserId);
                    if (subscriberDetails == null)
                        throw new DisbursementException("User details not found.");

                    if (request.NIN != subscriberDetails.NIN)
                        throw new BaseException(ExceptionType.CONFLICT,
                            "Supplied NIN does not match our records. " +
                            "Kindly update the subscriber's profile.");

                    var farmerImage = await _farmerRepository.GetFarmerImage(
                        query.RequestForUserId,
                        args.BusinessCode,
                        args.OrganizationCode);

                    if (farmerImage == null)
                        throw new DisbursementException(
                            "No KYC photo on record for this subscriber. Update the user's KYC photo.");

                    var faceResult = await _imageRecognitionService.CompareFaces(
                        request.UserImage,
                        new ImageVerificationArgs
                        {
                            OrganizationCode = args.OrganizationCode,
                            BusinessCode = args.BusinessCode,
                            FarmerUserId = query.RequestForUserId
                        });

                    if (faceResult == null || !faceResult.IsSimilarFace)
                        throw new DisbursementException(
                            $"Face verification failed " +
                            $"Captured Image at disbursement does not match existing user image. (similarity: {faceResult?.SimilarityPercentage ?? 0:P0}). " +
                            "Disbursement is not allowed.");

                    var agent = await _context.Agents
                        .FirstOrDefaultAsync(x => x.UserId == args.UserId);

                    if (agent == null)
                        throw new DisbursementException("No agent profile found for the current user.");
                    OrganizationLocation? disbursementLocation;

                    if (args.CurrentLoggedInUserRole == UserType.Agent.ToString())
                    {
                        var organizationAgent = await ResolveOrganizationAgentLocation(agent.Id, org.OrganizationCode, request.WarehouseId);

                        if (organizationAgent == null)
                            throw new DisbursementException(
                                "Agent does not have an active assignment within this organization tied to" +
                                "the selected warehouse" +
                                "and cannot perform disbursements.");


                        if (organizationAgent.Location.Id != request.WarehouseId)
                            throw new DisbursementException(
                                "Selected location does not match the agent's assigned location.");

                        disbursementLocation = organizationAgent.Location;

                    }
                    else if (args.CurrentLoggedInUserRole == UserType.MemberShip.ToString())
                    {
                        throw new BaseException(ExceptionType.OPERATION_FAILED, "Disbursement is not allowed for this user role at the moment.");
                        //disbursementLocation = await ResolveOrganizationUserLocation(org.OrganizationId, request.WarehouseId);
                    }
                    else
                    {
                        disbursementLocation = await ResolveOrganizationUserLocation(org.OrganizationId, request.WarehouseId);
                    }
                    double distanceMetres = GeometryHelper.HaversineDistanceMetres(
                        lat1: request.Latitude,
                        lon1: request.Longitude,
                        lat2: disbursementLocation.Latitude,
                        lon2: disbursementLocation.Longitude);

                    if (distanceMetres > GeometryHelper.AllowedRadiusMetres)
                    {
                        double excessMetres = distanceMetres - GeometryHelper.AllowedRadiusMetres;

                        throw new DisbursementException(
                            $"Location check failed for '{disbursementLocation.LocationName}'.\n" +
                            $"• Your distance from this location: {distanceMetres:N0} m\n" +
                            $"• Allowed range: {GeometryHelper.AllowedRadiusMetres:N0} m\n" +
                            $"• You need to move {excessMetres:N0} m closer to proceed.");
                    }


                    if (query.Status == DisbursementStatus.Disbursed)
                        throw new DisbursementException("This subscription has already been fully disbursed.");

                    var reference = ReferenceUtil.GenerateDisbursementReference();

                    var additionalCosts = query.Subscription.InputPackage.AdditionalCosts
                        ?? new List<AdditionalInputPackageCost>();

                    decimal paidToUserTotal = additionalCosts
                        .Where(x => x.AdditionalCostTreatmentType == AdditionalCostTreatmentType.PaidOutToUser)
                        .Sum(x => x.Amount);

                    decimal vendorPaidTotal = additionalCosts
                        .Where(x => x.AdditionalCostTreatmentType == AdditionalCostTreatmentType.VendorPaid)
                        .Sum(x => x.Amount);

                    var lineItems = new List<DisbursementLineItem>();

                    (decimal Amount, bool IsPartial) disbursement;

                    if (query.Subscription.InputPackage.PackageStructure == InputPackageStructure.Physical)
                    {
                        disbursement = await ProcessPhysicalDisbursementAsync(
                            query.Subscription, disbursementLocation, query,
                            request, args, reference,
                            paidToUserTotal, vendorPaidTotal,
                            lineItems);

                        if (query.Subscription.InputPackage.AdditionalCosts.Count > 0)
                        {
                            // disbursement.IsPartial = true;
                            //    var totalAdditionalCost = query.Subscription.InputPackage.AdditionalCosts.Sum(x => x.Amount);
                            //    //sum up the amount in the additional cost
                            //    disbursement = await ProcessMonetaryDisbursementAsync(
                            //     query.Subscription, agentLocation, query,
                            //     request, args, totalAdditionalCost, reference);
                        }  //revisit
                           // if physical but has additional cost to be given to farmer
                    }
                    else
                    {
                        throw new BaseException(ExceptionType.OPERATION_FAILED, "Monetary disbursement is currently not " +
                            "available! Kindly contact system administrator");
                        //disbursement = await ProcessMonetaryDisbursementAsync(
                        //    query.Subscription, agentLocation, query,
                        //    request, args, 0, reference);
                    }

                    var disbursedAmount = disbursement.Amount;
                    var isPartial = disbursement.IsPartial;

                    if (lineItems.Count > 0)
                        _context.DisbursementLineItems.AddRange(lineItems);

                    query.Status = DisbursementStatus.Disbursed;
                    query.DisbursedOn = DateTimeOffset.UtcNow;
                    query.ApprovedAmount = disbursedAmount;
                    query.DisbursedByUserId = args.UserId;
                    query.Reference = reference;          // use generated ref
                    query.IsPartial = isPartial;
                    query.LocationId = disbursementLocation.Id;
                    query.Latitude = request.Latitude;
                    query.Longitude = request.Longitude;

                    query.Subscription.Status = isPartial
                        ? SubscriptionStatus.PartiallyDisbursed
                        : SubscriptionStatus.Active;
                    query.Subscription.DisbursedOn = DateTimeOffset.UtcNow;

                    if (!query.Subscription.InputPackage.RepaymentRequired)
                    {
                        query.Subscription.Status = SubscriptionStatus.Completed;
                    }

                    _context.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        EventType = nameof(SubscriptionDisbursedEvent),
                        CreatedOn = DateTime.UtcNow,
                        IsProcessed = false,
                        Payload = JsonSerializer.Serialize(new SubscriptionDisbursedEvent
                        {
                            SubscriptionId = query.SubscriptionId,
                            PackageId = query.Subscription.InputPackageId,
                            Amount = disbursedAmount,
                            IsPartial = isPartial,
                            LocationId = disbursementLocation.Id,
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
                        LocationName = disbursementLocation.LocationName,
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
                            LocationId = x.OrganizationLocationId,
                            AgentUserId = x.AgentUserId,
                            Latitude = x.Latitude,
                            Longitude = x.Longitude,
                            CreatedOn = x.CreatedOn,
                        }).ToList()
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }



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
        private async Task<(decimal Amount, bool IsPartial)> ProcessPhysicalDisbursementAsync(
    InputPackageSubscription subscription,
    OrganizationLocation agentLocation,
    InputSubscriptionDisbursement approvedRequest,
    DisburseSubscriptionRequest request,
    SubscriptionQueryFilter args,
    string reference,
    decimal paidToUserTotal,
    decimal vendorPaidTotal,
    List<DisbursementLineItem> lineItems
    )
        {
            decimal itemsTotal = 0m;
            bool isPartial = false;
            var unavailableItems = new List<string>();

            foreach (var item in subscription.InputPackage.InputPackageItems)
            {
                var productName = item.OrganizationProduct?.ProductName ?? item.OrganizationProductId.ToString();

                var inventory = await _context.LocationInventories
                    .Include(x => x.OrganizationProduct)
                    .FirstOrDefaultAsync(x =>
                        x.OrganizationProductId == item.OrganizationProductId &&
                        x.LocationId == agentLocation.Id &&
                        x.IsActive == true);

                if (inventory == null)
                {
                    unavailableItems.Add($"'{productName}' is not stocked at '{agentLocation.LocationName}'");
                    continue;
                }

                if (inventory.Quantity == 0)
                {
                    unavailableItems.Add($"'{inventory.OrganizationProduct.ProductName}' is out of stock at '{agentLocation.LocationName}'");
                    continue;
                }

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
                    BusinessCode = args.BusinessCode,
                    OrganizationId = args.OrganizationId.Value,
                    LocationInventoryId = inventory.Id
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
                    OrganizationLocationId = agentLocation.Id,
                    AgentUserId = args.UserId,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    CreatedOn = DateTimeOffset.UtcNow,

                });

                itemsTotal += quantityToGive * item.UnitCost;
            }

            if (unavailableItems.Count > 0)
                throw new DisbursementException(
                    "Disbursement cannot proceed due to the following issue(s): " +
                    string.Join("; ", unavailableItems) + ".");

            var total = itemsTotal + paidToUserTotal + vendorPaidTotal;
            return (total, isPartial);
        }
        /// <summary>
        /// Handles the monetary path: checks available funds, deducts from the
        /// location's monetary inventory, and records a MonetaryDisbursementTransaction.
        /// Returns the total amount disbursed.
        /// </summary> 
        /// 
        private async Task<(decimal Amount, bool IsPartial)> ProcessMonetaryDisbursementAsync(
    InputPackageSubscription subscription,
    OrganizationLocation agentLocation,
    InputSubscriptionDisbursement approvedRequest,
    DisburseSubscriptionRequest request,
    SubscriptionQueryFilter args,
    decimal requestAmount,
    string reference)
        {
            bool isPartial = false;
            decimal subscribedAmount = subscription.InputPackage.TotalFinancingValue;

            //decimal availableFunds = await _inputFinancingRepository
            //    .GetAvailableBalanceAsync(agentLocation.Id);

            //if (availableFunds <= 0)
            //    throw new DisbursementException(
            //        $"No funds available at location '{agentLocation.LocationName}'.");

            decimal amountToDisburse = requestAmount > 0
                ? Math.Min(requestAmount, subscribedAmount)
                : subscribedAmount;

            decimal amountPending = subscribedAmount - amountToDisburse;
            if (amountPending > 0) isPartial = true;

            // Throws on failure/ambiguous outcome — nothing below this line runs
            // unless the wallet debit+credit both succeeded.
            await ProcessWalletDisbursementTransferAsync(
                subscription,
                args,
                amountToDisburse,
                reference,
                $"Disbursement for {subscription.InputPackage.Name} to {subscription.SubscriberUser.Name}");

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

            return (amountToDisburse, isPartial);
        }
        //private async Task<(decimal Amount, bool IsPartial)> ProcessMonetaryDisbursementAsync(
        //InputPackageSubscription subscription,
        //OrganizationLocation agentLocation,
        //InputSubscriptionDisbursement approvedRequest,
        //DisburseSubscriptionRequest request,
        //InputSubscriptionRequestArgs args,
        //decimal requestAmount,
        //string reference)
        //{
        //    bool isPartial = false;
        //    decimal subscribedAmount =  subscription.InputPackage.TotalFinancingValue;

        //    //decimal availableFunds = await _inputFinancingRepository
        //    //    .GetAvailableBalanceAsync(agentLocation.Id);

        //    //if (availableFunds <= 0)
        //    //    throw new DisbursementException(
        //    //        $"No funds available at location '{agentLocation.LocationName}'.");

        //    decimal amountToDisburse = Math.Min(requestAmount > 0 ? requestAmount : subscribedAmount); 
        //    decimal amountPending = subscribedAmount - amountToDisburse;
        //    if (amountPending > 0) isPartial = true;

        //    await ProcessWalletDisbursementTransferAsync(
        //        subscription,
        //        args,
        //        amountToDisburse,
        //        reference,
        //        $"Disbursement for {subscription.InputPackage.Name} to {subscription.SubscriberUser.Name}");

        //    await _inputFinancingRepository.DeductAsync(
        //        agentLocation.Id, amountToDisburse, reference, args.UserId);

        //    _context.MonetaryDisbursementTransactions.Add(new MonetaryDisbursementTransaction
        //    {
        //        DisbursementId = approvedRequest.Id,
        //        SubscriptionId = subscription.Id,
        //        LocationId = agentLocation.Id,
        //        SubscribedAmount = subscribedAmount,
        //        DisbursedAmount = amountToDisburse,
        //        PendingAmount = amountPending,
        //        DisbursedOn = DateTimeOffset.UtcNow,
        //        AgentUserId = args.UserId,
        //        Latitude = request.Latitude,
        //        Longitude = request.Longitude,
        //        ReferenceNumber = reference,
        //    });
        //    var total = amountToDisburse;
        //    return (total, isPartial);
        //}


        private async Task<WalletTransferResponse> ProcessWalletDisbursementTransferAsync(
    InputPackageSubscription subscription,
    SubscriptionQueryFilter args,
    decimal amountToDisburse,
    string reference,
    string description)
        {
            var organizationWalletProviderId = subscription.InputPackage.WalletProviderId;

            if (!organizationWalletProviderId.HasValue)
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "Disbursement cannot be performed. Organization wallet is not configured");

            var organizationWallet = await _userWalletRepository.GetUserWalletByProviderId(
                new UserWalletParams
                {
                    Id = organizationWalletProviderId.Value,
                    UserId = subscription.InputPackage.Organization.UserId,
                    businessCode = args.BusinessCode
                });

            if (organizationWallet == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Organization wallet not found");

            var subscriberWallet = await _userWalletRepository.GetUserWalletByProviderId(
                new UserWalletParams
                {
                    Id = organizationWalletProviderId.Value,
                    UserId = subscription.SubscriberUserId,
                    businessCode = args.BusinessCode
                });

            if (subscriberWallet == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Subscriber wallet not found");

            if (organizationWallet.WalletBalance < amountToDisburse)
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    $"Insufficient organization wallet balance. Kindly top up at least ₦{amountToDisburse:N0}");

            var masterReference = !string.IsNullOrWhiteSpace(reference)
                ? reference
                : $"{Guid.NewGuid():D}";

            var debitReference = $"{masterReference}-DB";
            var creditReference = $"{Guid.NewGuid():D}-CR";

            var provider = await _providerRepo.GetById(organizationWallet.WalletProviderId);

            if (provider == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Wallet provider not found");

            if (provider.Code != "Momo")
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    $"Disbursement not implemented for provider {provider.Code}");

            var providerParams = await RetrieveParams(provider.Code, args.Env, args.BusinessCode);
            var providerService = _factory.GetService(provider.Code);

            if (providerService == null)
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    $"Wallet service for {provider.Code} was not found");

            // ── STEP 1: Idempotency check ─────────────────────────────────────────
            // If a prior attempt already recorded this debit as complete, don't call
            // the provider again — a retry (manual or via reconciliation) must be safe.
            var existingDebit = await _walletTransactionRepository.GetWalletTransactionByRef(debitReference);
            if (existingDebit != null && existingDebit.WalletTransactionStatus == WalletTransactionStatus.DebitSuccessful)
            {
                // Debit already happened. Move straight to checking/attempting the credit.
                return await AttemptCreditAndFinalizeAsync(
                    subscription, args, subscriberWallet, organizationWallet,
                    provider, providerParams, providerService,
                    amountToDisburse, debitReference, creditReference,
                    masterReference, description);
            }

            // ── STEP 2: Record intent to debit (small independent commit) ─────────
            await _walletTransactionRepository.InitiateWalletWithdrawal(
                organizationWallet.Id, amountToDisburse, description, debitReference);

            // ── STEP 3: Call the provider — outside any DB transaction ────────────
            WalletResponseService<PaymentResponse>? debitResponse;
            try
            {
                var debitRequest = new WalletTopUpProviderRequest
                {
                    WalletNumber = organizationWallet.WalletNumber,
                    Amount = amountToDisburse,
                    Currency = organizationWallet.Currency,
                    Message = $"Approve withdrawal of {amountToDisburse:N0} for input disbursement",
                    TransactionReference = debitReference
                };

                debitResponse = await providerService.DebitWalletAsync(debitRequest, providerParams, args.Env);
            }
            catch (Exception ex)
            {
                // Provider threw / network error — outcome is UNKNOWN, not failed.
                // Do not assume failure; flag for reconciliation so a background job
                // checks the provider's own status endpoint before deciding anything.
                await _walletTransactionRepository.MarkPendingReconciliation(
                    debitReference, $"Debit call threw: {ex.Message}");

                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "We could not confirm the debit status. Our team has been notified and your request is being reconciled.");
            }

            if (debitResponse == null)
            {
                await _walletTransactionRepository.MarkPendingReconciliation(
                    debitReference, "Wallet debit request returned no response");

                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "We could not confirm the debit status. Our team has been notified and your request is being reconciled.");
            }

            var debitStatus = await _mtnMomoService.GetRequesToWithdrawStatus(debitReference, providerParams, args.Env);

            if (debitStatus == null)
            {
                await _walletTransactionRepository.MarkPendingReconciliation(
                    debitReference, "Could not confirm debit status after initiation");

                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "We could not confirm the debit status. Our team has been notified and your request is being reconciled.");
            }

            if (!debitStatus.IsSuccess)
            {
                await _walletTransactionRepository.CompleteWalletWithdrawal(
                    debitReference, false, null, debitStatus.Message ?? "Wallet debit failed");

                throw new BaseException(ExceptionType.OPERATION_FAILED, debitStatus.Message ?? "Wallet debit failed");
            }

            await _walletTransactionRepository.CompleteWalletWithdrawal(
                debitReference, true, debitResponse.Data.Reference, debitResponse.Data.Status);

            // ── STEP 4: Debit confirmed — proceed to credit ────────────────────────
            return await AttemptCreditAndFinalizeAsync(
                subscription, args, subscriberWallet, organizationWallet,
                provider, providerParams, providerService,
                amountToDisburse, debitReference, creditReference,
                masterReference, description);
        }

        private async Task<WalletTransferResponse> AttemptCreditAndFinalizeAsync(
            InputPackageSubscription subscription,
            SubscriptionQueryFilter args,
            UserWalletDetailResponseModel subscriberWallet,
            UserWalletDetailResponseModel organizationWallet,
            WalletProviderModel provider,
             WalletProviderParameterModel[] providerParams,
            IGenericWalletService providerService,
            decimal amountToDisburse,
            string debitReference,
            string creditReference,
            string masterReference,
            string description)
        {
            var existingCredit = await _walletTransactionRepository.GetWalletTransactionByRef(creditReference);
            if (existingCredit != null && existingCredit.WalletTransactionStatus == WalletTransactionStatus.Completed)// check for issuccessful
            {
                return new WalletTransferResponse
                {
                    IsSuccessful = true,
                    Amount = amountToDisburse,
                    ExternalReference = masterReference,
                    ProviderTransactionId = creditReference
                };
            }

            await _walletTransactionRepository.CreateWalletFundinAsync(
                subscriberWallet.Id, amountToDisburse, description, creditReference);

            WalletResponseService<PaymentResponse>? transferResponse;
            try
            {
                var transferRequest = new TransferRequest
                {
                    AccountNumber = subscriberWallet.WalletNumber,
                    Amount = $"{amountToDisburse}",
                    Currency = subscriberWallet.Currency,
                    Message = $"Disbursement for {subscription.InputPackage.Name} to {subscription.SubscriberUser.Name}",
                    Reference = creditReference,
                    SenderName = subscription.InputPackage.Organization.CompanyName
                        ?? subscription.InputPackage.Organization.FullName,
                    SenderAccountNumber = organizationWallet.WalletNumber,
                    RecipientName = subscription.SubscriberUser.Name
                };

                transferResponse = await providerService.InitiateWalletTransferAsync(transferRequest, providerParams, args.Env);
            }
            catch (Exception ex)
            {
                // Debit already succeeded. This is the "money left the org wallet,
                // outcome to the subscriber is unknown" case — never silently swallow it.
                await _walletTransactionRepository.MarkPendingReconciliation(
                    creditReference, $"Credit call threw after successful debit: {ex.Message}");

                await _walletTransactionRepository.FlagForReconciliation(
                    debitReference, creditReference,
                    $"Unexpected error during credit after successful debit: {ex.Message}");

                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "Funds were debited from the organization but the subscriber credit is unconfirmed. Our team has been notified to reconcile your transaction.");
            }

            if (transferResponse == null)
            {
                await _walletTransactionRepository.MarkPendingReconciliation(
                    creditReference, "Wallet transfer returned no response");

                await _walletTransactionRepository.FlagForReconciliation(
                    debitReference, creditReference,
                    "Credit returned no response after successful debit");

                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "Wallet service did not return a response. Our team has been notified to reconcile your transaction.");
            }

            var transferStatus = await _mtnMomoService.GetTransferStatus(creditReference, providerParams, args.Env);

            if (transferStatus == null)
            {
                await _walletTransactionRepository.MarkPendingReconciliation(
                    creditReference, "Could not confirm credit status after initiation");

                await _walletTransactionRepository.FlagForReconciliation(
                    debitReference, creditReference,
                    "Credit status could not be confirmed after successful debit");

                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "We could not confirm the credit status. Our team has been notified to reconcile your transaction.");
            }

            if (!transferStatus.IsSuccess)
            {
                var creditFailMessage = transferStatus.Message ?? "Wallet transfer failed";

                await _walletTransactionRepository.CompleteWalletFunding(
                    creditReference, false, null, creditFailMessage);

                await _walletTransactionRepository.FlagForReconciliation(
                    debitReference, creditReference,
                    $"Credit failed after successful debit: {creditFailMessage}");

                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    "Funds were debited from the organization but could not be credited to the subscriber. Our team has been notified to reconcile your transaction.");
            }

            await _walletTransactionRepository.CompleteWalletFunding(
                creditReference, true, transferResponse.Data.Reference, transferResponse.Data.Status);

            return new WalletTransferResponse
            {
                IsSuccessful = true,
                Amount = amountToDisburse,
                ExternalReference = masterReference,
                ProviderTransactionId = creditReference
            };
        }
        //private async Task<WalletTransferResponse> ProcessWalletDisbursementTransferAsync(
        //    InputPackageSubscription subscription,
        //    InputSubscriptionRequestArgs args,
        //    decimal amountToDisburse,
        //    string reference,
        //    string description)
        //{

        //    var organizationWalletProviderId = subscription.InputPackage.WalletProviderId;

        //    if (!organizationWalletProviderId.HasValue)
        //        throw new BaseException(
        //            ExceptionType.OPERATION_FAILED,
        //            "Disbursement cannot be performed. Organization wallet is not configured");

        //    var organizationWallet = await _userWalletRepository.GetUserWalletByProviderId(
        //        new UserWalletParams
        //        {
        //            Id = organizationWalletProviderId.Value,
        //            UserId = subscription.InputPackage.Organization.UserId,
        //            businessCode = args.BusinessCode
        //        });

        //    if (organizationWallet == null)
        //        throw new BaseException(ExceptionType.NOTFOUND, "Organization wallet not found");

        //    var subscriberWallet = await _userWalletRepository.GetUserWalletByProviderId(
        //        new UserWalletParams
        //        {
        //            Id = organizationWalletProviderId.Value,//subscription.SubscriberWalletProviderId,
        //            UserId = subscription.SubscriberUserId,
        //            businessCode = args.BusinessCode
        //        });

        //    if (subscriberWallet == null)
        //        throw new BaseException(ExceptionType.NOTFOUND, "Subscriber wallet not found");


        //    if (organizationWallet.WalletBalance < amountToDisburse)
        //        throw new BaseException(
        //            ExceptionType.OPERATION_FAILED,
        //            $"Insufficient organization wallet balance. Kindly top up at least ₦{amountToDisburse:N0}");


        //    var masterReference = !string.IsNullOrWhiteSpace(reference)
        //        ? reference
        //        : $"{Guid.NewGuid():D}";

        //    var debitReference = $"{masterReference}";
        //    var creditReference = $"{Guid.NewGuid():D}";


        //    var provider = await _providerRepo.Get(organizationWallet.WalletProviderId);

        //    if (provider == null)
        //        throw new BaseException(ExceptionType.NOTFOUND, "Wallet provider not found");

        //    var providerParams = await RetrieveParams(provider.Code, args.Env, args.BusinessCode);
        //    var providerService = _factory.GetService(provider.Code);

        //    if (providerService == null)
        //        throw new BaseException(
        //            ExceptionType.OPERATION_FAILED,
        //            $"Wallet service for {provider.Code} was not found");

        //    var strategy = _context.Database.CreateExecutionStrategy();

        //    return await strategy.ExecuteAsync(async () =>
        //    {
        //        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        //        try
        //        {

        //            await _walletTransactionRepository.InitiateWalletWithdrawal(
        //                organizationWallet.Id, amountToDisburse, description, $"{debitReference}-DB");

        //            string? debitProviderReference = null;
        //            string providerMessage = "";

        //            try
        //            {
        //                var debitRequest = new WalletTopUpProviderRequest
        //                {
        //                    WalletNumber = organizationWallet.WalletNumber,
        //                    Amount = amountToDisburse,
        //                    Currency = organizationWallet.Currency,
        //                    Message = $"Approve withdrawal of {amountToDisburse:N0} for input disbursement",
        //                    TransactionReference = debitReference
        //                };

        //                if (provider.Code == "Momo")
        //                {
        //                    var debitResponse = await providerService.DebitWalletAsync(
        //                        debitRequest, providerParams, args.Env);

        //                    if (debitResponse == null)
        //                    {
        //                        await _walletTransactionRepository.CompleteWalletWithdrawal(
        //                            $"{debitReference}-DB", false, null, "Wallet debit request returned no response");

        //                        throw new BaseException(
        //                            ExceptionType.OPERATION_FAILED,
        //                            "Could not initiate wallet debit");
        //                    }

        //                    var debitStatus = await _mtnMomoService.GetRequesToWithdrawStatus(
        //                        debitReference, providerParams, args.Env);

        //                    if (debitStatus == null || !debitStatus.IsSuccess)
        //                    {
        //                        var debitFailMessage = debitStatus?.Message ?? "Wallet debit failed";

        //                        await _walletTransactionRepository.CompleteWalletWithdrawal(
        //                            $"{debitReference}-DB", false, null, debitFailMessage);

        //                        throw new BaseException(ExceptionType.OPERATION_FAILED, debitFailMessage);
        //                    }

        //                    providerMessage = debitResponse.Data.Status;
        //                    debitProviderReference = debitResponse.Data.Reference;
        //                }
        //                else
        //                {
        //                    throw new BaseException(
        //                        ExceptionType.OPERATION_FAILED,
        //                        $"Debit not implemented for provider {provider.Code}");
        //                }

        //                await _walletTransactionRepository.CompleteWalletWithdrawal(
        //                    $"{debitReference}-DB", true, debitProviderReference, providerMessage);
        //            }
        //            catch (BaseException)
        //            {
        //                await dbTransaction.RollbackAsync();
        //                throw;
        //            }
        //            catch (Exception ex)
        //            {
        //                await _walletTransactionRepository.CompleteWalletWithdrawal(
        //                    $"{debitReference}-DB", false, null, ex.Message);

        //                await dbTransaction.RollbackAsync();

        //                throw new BaseException(
        //                    ExceptionType.OPERATION_FAILED,
        //                    $"An unexpected error occurred during wallet debit: {ex.Message}");
        //            }

        //            // ─── Credit subscriber wallet ───

        //            await _walletTransactionRepository.CreateWalletFundinAsync(
        //                subscriberWallet.Id, amountToDisburse, description, $"{creditReference}-CR");

        //            try
        //            {
        //                var transferRequest = new TransferRequest
        //                {
        //                    AccountNumber = subscriberWallet.WalletNumber,
        //                    Amount = $"{amountToDisburse}",
        //                    Currency = subscriberWallet.Currency,
        //                    Message = $"Disbursement for {subscription.InputPackage.Name} to {subscription.SubscriberUser.Name}",
        //                    Reference = creditReference,
        //                    SenderName = subscription.InputPackage.Organization.CompanyName
        //                        ?? subscription.InputPackage.Organization.FullName,
        //                    SenderAccountNumber = organizationWallet.WalletNumber,
        //                    RecipientName = subscription.SubscriberUser.Name
        //                };

        //                var transferResponse = await providerService.InitiateWalletTransferAsync(
        //                    transferRequest, providerParams, args.Env);

        //                if (transferResponse == null)
        //                {
        //                    await _walletTransactionRepository.CompleteWalletFunding(
        //                        $"{creditReference}-CR", false, null, "Wallet transfer returned no response");

        //                    // Debit already succeeded externally, commit what we have so the
        //                    // reconciliation flag is persisted, then throw.
        //                    await dbTransaction.CommitAsync();

        //                    await _walletTransactionRepository.FlagForReconciliation(
        //                        $"{debitReference}-DB", $"{creditReference}-CR",
        //                        "Credit returned no response after successful debit");

        //                    throw new BaseException(
        //                        ExceptionType.OPERATION_FAILED,
        //                        "Wallet service did not return a response. Our team has been notified to reconcile your transaction.");
        //                }

        //                if (provider.Code == "Momo")
        //                {
        //                    var transferStatus = await _mtnMomoService.GetTransferStatus(
        //                        creditReference, providerParams, args.Env);

        //                    if (transferStatus == null || !transferStatus.IsSuccess)
        //                    {
        //                        var creditFailMessage = transferStatus?.Message ?? "Wallet transfer failed";

        //                        await _walletTransactionRepository.CompleteWalletFunding(
        //                            $"{creditReference}-CR", false, null, creditFailMessage);

        //                        // Commit partial state so reconciliation flag persists
        //                        await dbTransaction.CommitAsync();

        //                        await _walletTransactionRepository.FlagForReconciliation(
        //                            $"{debitReference}-DB", $"{creditReference}-CR",
        //                            $"Credit failed after successful debit: {creditFailMessage}");

        //                        throw new BaseException(
        //                            ExceptionType.OPERATION_FAILED,
        //                            "Funds were debited from the organization but could not be credited to the subscriber. Our team has been notified to reconcile your transaction.");
        //                    }
        //                }
        //                else
        //                {
        //                    await dbTransaction.RollbackAsync();

        //                    throw new BaseException(
        //                        ExceptionType.OPERATION_FAILED,
        //                        $"Credit not implemented for provider {provider.Code}");
        //                }

        //                await _walletTransactionRepository.CompleteWalletFunding(
        //                    $"{creditReference}-CR", true,
        //                    transferResponse.Data.Reference,
        //                    transferResponse.Data.Status);

        //                // Commit everything 
        //                await dbTransaction.CommitAsync();

        //                return new WalletTransferResponse
        //                {
        //                    IsSuccessful = true,
        //                    Amount = amountToDisburse,
        //                    ExternalReference = masterReference,
        //                    ProviderTransactionId = $"{creditReference}-CR"
        //                };
        //            }
        //            catch (BaseException)
        //            {
        //                throw;
        //            }
        //            catch (Exception ex)
        //            {
        //                await _walletTransactionRepository.CompleteWalletFunding(
        //                    creditReference, false, null, ex.Message);

        //                await dbTransaction.CommitAsync();

        //                await _walletTransactionRepository.FlagForReconciliation(
        //                    debitReference, creditReference,
        //                    $"Unexpected error during credit after successful debit: {ex.Message}");

        //                throw new BaseException(
        //                    ExceptionType.OPERATION_FAILED,
        //                    "An unexpected error occurred while crediting the subscriber. Our team has been notified to reconcile your transaction.");
        //            }
        //        }
        //        catch (BaseException)
        //        {
        //            if (_context.Database.CurrentTransaction != null)
        //                await dbTransaction.RollbackAsync();

        //            throw;
        //        }
        //        catch (Exception ex)
        //        {
        //            if (_context.Database.CurrentTransaction != null)
        //                await dbTransaction.RollbackAsync();

        //            throw new BaseException(
        //                ExceptionType.OPERATION_FAILED,
        //                $"An unexpected error occurred: {ex.Message}");
        //        }
        //    });
        //}


        #endregion

        #region reporting

        #endregion
    }
}
