using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Utilities;
using Azure.Core;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Ocsp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public interface IInputFinancingSchemeManager
    {
        Task<(bool Success, string Message)> CreateSchemeAsync(InputFinancingSchemeDto model, BusinessRegistrationArgs args);
        Task<PagedListResponse<List<InputFinancingSchemeDto>>> GetAllAsync(BusinessRegistrationArgs args);
        Task<InputFinancingSchemeDto> GetByIdAsync(long id);
        Task UpdateAsync(long id, CreateInputFinancingSchemeDto dto);
        Task DeleteAsync(long id);
        Task<ResponseDto> CreateSubscription(InputSubscriptionRequestDto request, BusinessRegistrationArgs args);
        Task<ResponseDto> VerifyAndActivateAsync(BusinessRegistrationArgs args, long subscriptionId, string verifiedBy);
        Task<ResponseDto> GetPendingVerificationAsync();
        Task<ResponseDto> GetByFarmerAsync(string farmerId);
        Task<ResponseDto> GetAllSubscriptionsAsync(BusinessRegistrationArgs query);
    }
    public class InputFinancingSchemeManager : IInputFinancingSchemeManager
    {
        private readonly IInputFinancingSchemeRepository _repository;
        private readonly IOrganizationConfigurationRepository _configRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly ILogger<InputFinancingSchemeManager> _log;
        private readonly IWalletRepository _walletRepo;
        private readonly IMembershipRepository _cooperativeRepository;


        private readonly IGoogleDriveService _fileUploadService;
        public InputFinancingSchemeManager(
            IInputFinancingSchemeRepository repository,
            IBusinessRepository businessRepository,
            IOrganizationConfigurationRepository configRepository, ILogger<InputFinancingSchemeManager> log,
            IWalletRepository walletRepository,
            IMembershipRepository cooperativeRepository
          ,
            IGoogleDriveService fileUploadService
            )
        {
            _repository = repository;
            _configRepository = configRepository;
            _businessRepository = businessRepository;
            _log = log;
            _walletRepo = walletRepository;
            _cooperativeRepository = cooperativeRepository;
            //_fileUploadService = fileUploadService;
        }

        public async Task<(bool Success, string Message)> CreateSchemeAsync(InputFinancingSchemeDto model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business does not exist.");
            var org = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
            if (org == null) { throw new Exception($"Organization not found"); }

            
            if (string.IsNullOrWhiteSpace(model.SchemeName))
                return (false, "Scheme name is required.");

            if (model.TotalValue <= 0)
                return (false, "Total amount must be greater than zero.");

            if (model.RepaymentStartDate <= DateTime.UtcNow)
                return (false, "Repayment start date must be in the future.");

            if (await _repository.ExistsByNameAsync(model.SchemeName))
                return (false, $"A scheme with the name '{model.SchemeName}' already exists.");

            // 🔹 Validate configuration compatibility
            var config = await _configRepository.GetInputConfigByCodeAsync(model.ConfigurationCode);
            if (config == null)
                return (false, "Invalid configuration code.");

            var subscription = config.SubscriptionConfig;
            if (subscription == null)
                return (false, "Invalid or missing subscription configuration.");

            // ✅ Validate Contribution Split
            var (isValid, message) = ConfigurationValidator.ValidateConfiguration(config);
            if (!isValid)
                return (false, message);

            // ✅ Validate compatibility with beneficiary type
            if (model.BeneficiaryType == "Farmer" && !subscription.AllowFarmerSelfSubscription)
                return (false, "This configuration does not support farmer-based subscription.");

            if (model.BeneficiaryType == "Cooperative" && !subscription.AllowCooperativeSubscription)
                return (false, "This configuration does not support cooperative-based subscription.");
            // 🔹 Save
            var entity = new InputFinancingScheme
            {
                SchemeCode = model.SchemeCode,
                Season = model.Season,
                SubscriptionCloseDate = model.SubscriptionCloseDate,
                SubscriptionOpenDate = model.SubscriptionOpenDate,
                RepaymentEndDate = model.RepaymentEndDate,
                BusinessCode = args.businessCode,
                OrganizationCode = args.organizationCode,
                FundingSource = model.FundingSource,
                InputFinancingConfigurationId = config.Id,
                IsActive = true,
                //Targets= model.Targets?.Select(t => new SchemeTarget
                //{
                //    TargetId = t.TargetId,
                //    TargetType = t.TargetType
                //}).ToList(),    
                UnitValuePerHectare = model.UnitValuePerHectare,
                //  WarehouseId = model.WarehouseId,
                SchemeName = model.SchemeName,
                Description = model.Description,
                BeneficiaryType = model.BeneficiaryType,
                TotalValue = model.TotalValue,
                TotalArea = model.TotalArea,
                Commodity = model.Commodity,
                RepaymentStartDate = model.RepaymentStartDate,
                InputFinancingConfigurationCode = model.ConfigurationCode,
                CreatedBy = args.UserId,
                CreatedOn = model.CreatedOn
            };

            await _repository.AddAsync(entity);
            return (true, "Input financing scheme created successfully.");
        }
        public async Task<PagedListResponse<List<InputFinancingSchemeDto>>> GetAllAsync(BusinessRegistrationArgs query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }
                var schemes = await _repository.GetAllAsync(query);
                var result = schemes.Select(MapToDto);

                return new PagedListResponse<List<InputFinancingSchemeDto>>
                {
                    Succeeded = true,
                    Data = result.ToList(),
                    Count = result.ToList().Count, //result.Count() ?? 0,
                    Message = result == null || result.ToList().Count == 0 ? "No Record Found" : "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError($"Unexpected error retrieving Agents: {ex.Message}");
                throw new NotFoundException("No Record Found");
            }
        }

        public async Task<InputFinancingSchemeDto> GetByIdAsync(long id)
        {
            var scheme = await _repository.GetByIdAsync(id);
            return MapToDto(scheme);
        }

        public async Task UpdateAsync(long id, CreateInputFinancingSchemeDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) throw new Exception("Scheme not found");

            existing.SchemeName = dto.SchemeName;
            existing.Description = dto.Description;
            existing.Commodity = dto.Commodity;
            existing.TotalValue = dto.TotalValue;
            existing.IsActive = dto.IsActive;

            await _repository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(long id)
        {
            await _repository.DeleteAsync(id);
        }
      
        public async Task<ResponseDto> CreateSubscription(InputSubscriptionRequestDto request, BusinessRegistrationArgs args)
        {
            MemberShipModel coopRepo = new MemberShipModel();
            if (request.SubscribedBy == "Cooperative")
            {
                 coopRepo = await _cooperativeRepository.GetCooperativeDetailByUserId(request.SubscriberUserId);
                if (coopRepo == null)
                    return ResponseDto.Failure("Cooperative Detail not found");

                
            }

            var orgConfig = await _configRepository.GetByCodeAsync(args.organizationCode);
            if (orgConfig == null)
                return ResponseDto.Failure("Organization not found");

            var defaultOrgWallet = orgConfig.WalletProviderConfiguration.FirstOrDefault(x => x.IsDefault);
           
            var scheme = await _repository.GetByIdAsync(request.SchemeId);
            if (scheme == null)
                return ResponseDto.Failure("Invalid scheme selected.");

            var config = await _configRepository.GetInputConfigByCodeAsync(request.ConfigurationCode);
            if (config == null)
                return ResponseDto.Failure("Invalid configuration code.");



            if (config.ConfigurationCode != scheme.InputFinancingConfigurationCode)
                return ResponseDto.Failure("Configuration does not match the selected scheme.");


            var subConfig = config.SubscriptionConfig;
            var repayConfig = config.RepaymentConfig;

            var now = DateTimeOffset.UtcNow;
            if (!scheme.IsActive)
                return ResponseDto.Failure("This scheme is inactive.");



            if (now < scheme.SubscriptionOpenDate)
                return ResponseDto.Failure($"Subscription not yet open. Opens on {scheme.SubscriptionOpenDate:MMM dd, yyyy}.");


            if (now > scheme.SubscriptionCloseDate)
                return ResponseDto.Failure($"Subscription closed on {scheme.SubscriptionCloseDate:MMM dd, yyyy}.");


            if (request.SubscribedBy == "farmer" && !subConfig.AllowFarmerSelfSubscription)
                return ResponseDto.Failure("This scheme only allows cooperative subscriptions.");



            if (request.SubscribedBy == "cooperative" && !subConfig.AllowCooperativeSubscription)
                return ResponseDto.Failure("Cooperative subscriptions are not permitted for this scheme.");


            var totalValue = scheme.TotalValue;


            decimal equityAmount = 0;
            decimal repaymentAmount = 0;

            if (subConfig.RequiresEquity)
            {
                if (subConfig.IsEquityInPercentage)
                    equityAmount = (decimal)(totalValue * subConfig.EquityPercentage / 100);
                else if (subConfig.IsEquityStaticFee)
                    equityAmount = subConfig.EquityStaticFee;
            }

            if (repayConfig.IsRepaymentInPercentage)
                repaymentAmount = (decimal)(totalValue * repayConfig.ExpectedRepaymentPercentage / 100);
            else if (repayConfig.IsRepaymentStaticFee)
                repaymentAmount = repayConfig.RepaymentStaticFee;
            else
                repaymentAmount = totalValue;

            string? paymentProofUrl = null;

            if (subConfig.RequiresEquity)
            {
                if (!request.AgreeToDebit && request.PaymentProof == null && string.IsNullOrWhiteSpace(request.TransactionRef))
                    return ResponseDto.Failure("Please agree to debit wallet or provide payment evidence/transaction reference.");


                if (request.AgreeToDebit)
                {
                    var wallet = await _walletRepo.GetUserWalletByProviderAsync(request.SubscriberUserId, defaultOrgWallet.DefaultWalletProvider);
                    if (wallet == null)
                        return ResponseDto.Failure("Wallet not found for farmer.");


                    if (wallet.WalletBalance < equityAmount)
                        return ResponseDto.Failure("Insufficient wallet balance. Please fund your wallet.");



                    await _walletRepo.DebitAsync(wallet.Id, equityAmount, "Equity payment for input subscription");
                }

                if (request.PaymentProof != null)
                    paymentProofUrl = await _fileUploadService.UploadFileAsync(request.PaymentProof, "subscription_proofs");
            }

            DateTimeOffset repaymentStart = scheme.RepaymentStartDate != default
      ? scheme.RepaymentStartDate
      : DateTimeOffset.UtcNow.AddMonths(1);

            if (repayConfig.RepaymentDurationInMonths > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(repayConfig.RepaymentDurationInMonths),
                    "Repayment duration is too large.");

            DateTimeOffset repaymentEnd = scheme.RepaymentEndDate != default
                ? scheme.RepaymentEndDate
                : repaymentStart.AddMonths((int)repayConfig.RepaymentDurationInMonths);
           
            
            var subscription = new InputSubscription
            {
                SubscribedByUserId = request.SubscriberUserId,
                CooperativeId=coopRepo != null ? coopRepo.Id : 0,
                InputSchemeId = request.SchemeId,
                ConfigurationCode = request.ConfigurationCode,
                SubscriberType = request.SubscribedBy,
                OrganizationCode = args.organizationCode,
                BusinessCode=args.businessCode,
                EquityAmount = equityAmount,
                RepaymentAmount = repaymentAmount,

                PaymentProofUrl = paymentProofUrl,
                TransactionRef = request.TransactionRef,
                RepaymentStartDate = repaymentStart,
                RepaymentEndDate = repaymentEnd,
                CreatedOn = now,
                Status = "ActiveSubscription"
            };

            await _repository.AddInputSubscription(subscription);

            return ResponseDto.Success("Subscription created successfully.", new
            {
                subscription.Id,
                subscription.EquityAmount,
                subscription.RepaymentAmount,
                subscription.RepaymentStartDate,
                subscription.RepaymentEndDate
            });
        }


        public async Task<ResponseDto> GetAllSubscriptionsAsync(BusinessRegistrationArgs query)
        {
            var subscriptions = await _repository.GetAllInputSubscriptionAsync(query);
            return ResponseDto.Success("Subscriptions retrieved successfully.", subscriptions);
        }

        // ==============================
        // 3️⃣ Retrieve By Farmer
        // ==============================
        public async Task<ResponseDto> GetByFarmerAsync(string farmerId)
        {
            var subscriptions = await _repository.GetInputSubscriptionByUserIdAsync(farmerId);
            return ResponseDto.Success("Farmer subscriptions retrieved successfully.", subscriptions);
        }

        // ==============================
        // 4️⃣ Retrieve Pending Verification
        // ==============================
        public async Task<ResponseDto> GetPendingVerificationAsync()
        {
            var pending = await _repository.GetSubscriptionByStatusAsync("PendingVerification");
            return ResponseDto.Success("Pending subscriptions retrieved successfully.", pending);
        }

        // ==============================
        // 5️⃣ Verify Payment Proof & Activate
        // ==============================
        public async Task<ResponseDto> VerifyAndActivateAsync(BusinessRegistrationArgs args,long subscriptionId, string verifiedBy)
        {
            var subscription = await _repository.GetInputSubscriptionByIdAsync(subscriptionId);
            if (subscription == null)
                return ResponseDto.Failure("Subscription not found.");

            if (subscription.Status != "PendingVerification")
                return ResponseDto.Failure("Subscription is not awaiting verification.");
            var orgConfig = await _configRepository.GetByCodeAsync(args.organizationCode);
            if (orgConfig == null)
                return ResponseDto.Failure("Organization not found");

            var defaultOrgWallet = orgConfig.WalletProviderConfiguration.FirstOrDefault(x => x.IsDefault);

            var wallet = await _walletRepo.GetUserWalletByProviderAsync(subscription.SubscribedByUserId, defaultOrgWallet.DefaultWalletProvider);

            if (wallet == null)
                return ResponseDto.Failure("Farmer wallet not found.");

            if (wallet.WalletBalance < subscription.EquityAmount)
                return ResponseDto.Failure("Insufficient wallet balance to activate subscription.");

            await _walletRepo.DebitAsync(wallet.Id, subscription.EquityAmount, "Equity payment verified and debited");
            await _repository.UpdateSubscriptionStatusAsync(subscriptionId, "Active", verifiedBy);

            return ResponseDto.Success("Subscription verified and activated successfully.");
        }
        private InputFinancingSchemeDto MapToDto(InputFinancingScheme scheme)
        {
            if (scheme == null) return null;

            return new InputFinancingSchemeDto
            {
                Id = scheme.Id,
                SchemeName = scheme.SchemeName,
                SchemeCode = scheme.SchemeCode,
                ConfigurationCode = scheme.InputFinancingConfigurationCode,
                CreatedBy = scheme.CreatedBy,
                CreatedOn = scheme.CreatedOn,
                Description = scheme.Description,
                Commodity = scheme.Commodity,
                BeneficiaryType = scheme.BeneficiaryType,
                TotalArea = scheme.TotalArea,
                TotalValue = scheme.TotalValue,
                FundingSource = scheme.FundingSource,
                Season = scheme.Season,
                //WarehouseId = scheme.WarehouseId,
                InputFinancingConfigurationId = scheme.InputFinancingConfigurationId,
                IsActive = scheme.IsActive,
                UnitValuePerHectare = scheme.UnitValuePerHectare,
                RepaymentStartDate = scheme.RepaymentStartDate,
                RepaymentEndDate = scheme.RepaymentEndDate,
                SubscriptionOpenDate = scheme.SubscriptionOpenDate,
                SubscriptionCloseDate = scheme.SubscriptionCloseDate,
                OrganizationCode = scheme.OrganizationCode,
                BusinessCode = scheme.BusinessCode,
                //Targets = scheme.Targets?.Select(t => new SchemeTargetDto
                //{
                //    TargetId = t.TargetId,
                //    TargetType = t.TargetType
                //}).ToList()
            };
        }
    }
}
