using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Agrovesto.Backend.Core.Models.Businesses.Input
{
    public class GenerateInputPackageRequest
    {
        public long EconomicsOfProductionId { get; set; }
        public long OrganizationId { get; set; }
        public long CategoryId { get; set; }

        public InputSubScriptionType SubscriptionType { get; set; }

        public bool CommitmentRequired { get; set; }
        public CommitmentType CommitmentType { get; set; }
        public decimal CommitmentValue { get; set; }

        public InputPackageRepaymentMode RepaymentMode { get; set; }

        public int RepaymentPeriodInMonths { get; set; }

        public bool ApplyInterest { get; set; }
        public decimal InterestRate { get; set; }

        public string Currency { get; set; } = "NGN";

        public bool AutoApprove { get; set; } = false;
    }

    public class EopResponseDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Crop { get; set; }

        public decimal LandSize { get; set; }

        public string UnitOfMeasure { get; set; }

        public decimal ExpectedYield { get; set; }

        public decimal ExpectedMarketPrice { get; set; }

        public decimal TotalCostOfProduction { get; set; }

        public decimal ExpectedRevenue { get; set; }

        public decimal ExpectedProfit { get; set; }

        public string Season { get; set; }

        public long OrganizationId { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public List<EopCostItemResponseDto> CostItems { get; set; } = new();
    }

    public class EopCostItemResponseDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitCost { get; set; }

        public decimal TotalCost { get; set; }

        public long? OrganizationProductId { get; set; }
    }
    public class CreateEopRequest
    {
        public string Name { get; set; }
        public string Crop { get; set; }
        public string Season { get; set; }

        public decimal LandSize { get; set; }

        public decimal ExpectedYield { get; set; }
        public decimal ExpectedMarketPrice { get; set; }

        public long OrganizationId { get; set; }
        public string? UnitOfMeasure { get; set; }

        public List<CreateEopCostItemRequest> CostItems { get; set; }
    }
    public class CreateEopCostItemRequest
    {
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }

        public long? OrganizationProductId { get; set; } // null = service cost
    }

    public class InputPackageConfigArgs
    {
        public long organizationId { get; set; }
        public bool isSelectedWalletProvisioned { get; set; } = false;
        public string? createdByUserId { get; set; }

        public string? OrganizationCode { get; set; }

        public string BusinessCode { get; set; }
    }
    public class CreateInputPackageRequest
    {
        public string Name { get; set; }
        public long InputFinancingCategoryId { get; set; }
        public InputSubScriptionType SubScriptionType { get; set; }
        public string InputTarget { get; set; } // e.g. crop, livestock, fishery
        public string InputSeason { get; set; } // e.g. 2024 wet season
        public InputPackageStructure PackageStructure { get; set; }
        public List<CreateInputPackageItemRequest> InputPackageItems { get; set; } = new();
        public decimal TotalPackageValue { get; set; } //calculate if structure is physical
        public bool CommitmentRequired { get; set; }
        public bool RepaymentRequired { get; set; }
        public CommitmentType CommitmentType { get; set; }
        public decimal CommitmentValue { get; set; } //if %, commitmentvalue = % of total package value, if fixed, commitmentvalue = fixed amount

        public DateTimeOffset? CommitmentPaymentStartDate { get; set; }
        public DateTimeOffset? CommitmentPaymentEndDate { get; set; }

        public long PaybackWalletId { get; set; } // wallet provider type, e.g etransact, momo, etc
        public long? UserWalletId { get; set; }
        public RepaymentFrequency RepaymentFrequency { get; set; }
        public int RepaymentDuration { get; set; } //e.g 1 week, 4 month, 1 day etc
        public DateTimeOffset? RepaymentStartDate { get; set; }
        public DateTimeOffset? RepaymentEndDate { get; set; }
        public DateTimeOffset? RepaymentGracePeriod { get; set; }
        public InputPackageRepaymentMode RepaymentModel { get; set; }
        public decimal RepaymentValue { get; set; } = 0;
        public RiskCategory RiskCategory { get; set; }



        public List<CreateAdditionalCostRequest> AdditionalCosts { get; set; } = new();

        public decimal TotalFinancingValue { get; set; }










        //public bool ApplyInterest { get; set; }
        //public decimal InterestRate { get; set; }

        public string Currency { get; set; } = "NGN";
    }
    public class CreateInputPackageItemRequest
    {
        public long OrganizationProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string? UnitOfMeasure { get; set; }
    }
    public class CreateAdditionalCostRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public AdditionalCostType Type { get; set; } = AdditionalCostType.Other;
        public bool IsPerUnit { get; set; } // e.g. per hectare
        public decimal? Quantity { get; set; }
    }


    public class UpdateInputPackageConfigRequest
    {
        public bool? CommitmentRequired { get; set; }
        public bool? RepaymentRequired { get; set; }

        public CommitmentType? CommitmentType { get; set; }
        public decimal? CommitmentValue { get; set; }
        public DateTimeOffset? CommitmentPaymentStartDate { get; set; }
        public DateTimeOffset? CommitmentPaymentEndDate { get; set; }

        public RepaymentFrequency? RepaymentFrequency { get; set; }
        public int? RepaymentDuration { get; set; }
        public DateTimeOffset? RepaymentStartDate { get; set; }
        public DateTimeOffset? RepaymentEndDate { get; set; }
        public DateTimeOffset? RepaymentGracePeriod { get; set; }
        public InputPackageRepaymentMode? RepaymentModel { get; set; }
        public decimal? RepaymentValue { get; set; }
        public RiskCategory? RiskCategory { get; set; }
    }//views
    public class EopDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Crop { get; set; }
        public string Season { get; set; }

        public decimal LandSize { get; set; }
        public decimal ExpectedYield { get; set; }
        public decimal ExpectedMarketPrice { get; set; }

        public decimal TotalCostOfProduction { get; set; }

        public List<EopCostItemDto> CostItems { get; set; }
    }
    public class EopCostItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }

        public long? OrganizationProductId { get; set; }
    }
    public class InputPackageDto
    {
        public long WalletProviderId { get; set; }
        public bool IsActive { get; set; }
        public long Id { get; set; }
        public string Name { get; set; }
        public string FinancingCategory { get; set; }
        public string InputTarget { get; set; } // e.g. crop, livestock, fishery
        public string InputSeason { get; set; } // e.g. 2024 wet season
        public string SubscriptionType { get; set; }
        public bool IsRepaymentWalletConfigured { get; set; }
        public InputPackageFinancingStructureDto FinancialStructure { get; set; }
        public InputPackageRepaymentWallet RepaymentWallet { get; set; }

        public List<InputPackageBreakdownDto> PackageInputs { get; set; }
        public List<AdditionalCostDto> AdditionalCosts { get; set; }
        public List<RepaymentScheduleDto> RepaymentBreakdown { get; set; }
    }
    public class UpdateRepaymentPlanRequest
    {
        public RepaymentFrequency? RepaymentFrequency { get; set; }
        public int? RepaymentDuration { get; set; }
        public DateTimeOffset? RepaymentStartDate { get; set; }
        public DateTimeOffset? RepaymentEndDate { get; set; }
        public DateTimeOffset? RepaymentGracePeriod { get; set; }
        public InputPackageRepaymentMode? RepaymentModel { get; set; }
        public decimal? RepaymentValue { get; set; }
        public RiskCategory? RiskCategory { get; set; }
    }

    public class EditRepaymentPlanResult
    {
        public bool Success { get; set; }
        public int SubscribersRegenerated { get; set; }
        public List<long> SubscribersRequiringManualReview { get; set; } = new();
    }
    public class InputPackageFinancingStructureDto
    {
        public decimal TotalFinancingValue { get; set; }
        public bool CommitmentRequired { get; set; }
        public decimal CommitmentValue { get; set; }
        public decimal CommitmentPercentage { get; set; }
        public decimal RepaymentValue { get; set; }
        public DateTimeOffset? CommitmentPaymentStartDate { get; set; }
        public DateTimeOffset? CommitmentPaymentEndDate { get; set; }
        //  public long RepaymentDuration { get; set; } // calculate the frequency and the supplied duration e.g 6 months, 1 year, etc
        public DateTimeOffset? RepaymentStartDate { get; set; }
        public DateTimeOffset? RepaymentEndDate { get; set; }
        public DateTimeOffset? RepaymentGracePeriod { get; set; }
        public string RepaymentModel { get; set; }
        public string RiskCategory { get; set; }
        public string RepaymentPeriod { get; set; }
    }
    public class InputPackageRepaymentWallet
    {
        public long WalletId { get; set; }
        public string WalletProvider { get; set; }
        public string WalletName { get; set; }
        public string WalletNumber { get; set; }

    }
    public class InputPackageBreakdownDto
    {
        public string InputType { get; set; }
        public long InputId { get; set; }
        public long OrganizationProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class InputPackageItemDto
    {
        public long OrganizationProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
    public class AdditionalCostDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
    }
    public class PagedResult<T>
    {
        public List<T>? Data { get; set; }
        public int Count { get; set; }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)Count / PageSize);
    }
    public class EopFilterRequest : PagedRequest
    {
        public long OrganizationId { get; set; }
        public string? Crop { get; set; }
        public string? Season { get; set; }
    }
    public class PagedRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
    public class InputPackageFilterRequest : PagedRequest
    {
        public long OrganizationId { get; set; }

        public InputPackageStatus? Status { get; set; }
        public bool? IsGeneratedFromEop { get; set; }
        public long? CategoryId { get; set; }
    }


    public class InputPackageListModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string FinancingCategory { get; set; }
        public string InputTarget { get; set; } // e.g. crop, livestock, fishery
        public decimal TotalFinancingValue { get; set; } //calculate if structure is physical
        public decimal CommitmentValue { get; set; } //if %, commitmentvalue = % of total package value, if fixed, commitmentvalue = fixed amount

        public string RepaymentModel { get; set; }
        public string RiskCategory { get; set; }

        public long Subscribers { get; set; }
        public bool IsActive { get; set; }
        public string SubscriberType { get; set; }
    }
    public class ReplacePackageItemRequestArgs
    {
        public long PackageId { get; set; }
        public long AdditionalCostId { get; set; }
        public string BusinessCode { get; set; }
        public long InputId { get; set; }
        public string? OrganizationCode { get; set; }
        public long OrganizationId { get; set; }

    }

    public class ReplacePackageItemRequest
    {
        [Required]
        public long PackageId { get; set; }

        [Required]
        public long ItemId { get; set; }

        [Required]
        public long OrganizationProductId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        public string? UnitOfMeasure { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal UnitCost { get; set; }

    }


    public class RepaymentEngineRequest
    {
        public long InputSubscriptionId { get; set; }
        public RepaymentFrequency Frequency { get; set; }
        public int Duration { get; set; }

        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }

        public DateTimeOffset? GracePeriod { get; set; }

        public decimal TotalAmount { get; set; }
    }

    public class AddSubscriberRequest
    {
        [Required]
        public long PackageId { get; set; }

        [Required]
        public string SubscriberUserId { get; set; }

        [Required]
        [EnumDataType(typeof(InputSubScriptionType))]
        public InputSubScriptionType SubscriberType { get; set; } = InputSubScriptionType.Individual;
    }
    public class InputSubscriptionRequestArgs
    {
        public string? UserId { get; set; }
        public string? OrganizationCode { get; set; }
        public long? OrganizationId { get; set; }

        public string? CurrentLoggedInUserRole { get; set; }
        public string BusinessCode { get; set; }
        public long SubscriptionId { get; set; }
        public long PackageId { get; set; }
        public string? SubscriberUserId { get; set; }
        public string? Env { get; set; }
    }



    public class ApproveSubscriptionRequest
    {
        public long SubscriptionId { get; set; }

        [Required]
        [EnumDataType(typeof(ApprovalDecision))]
        public ApprovalDecision ApprovalState { get; set; }

        public string? RejectionReason { get; set; }
    }
    public class RecordCommitmentPaymentRequest
    {
        public long SubscriptionId { get; set; }
        public string? SubscriberUserId { get; set; }

        public decimal Amount { get; set; }

        public PaymentChannel PaymentChannel { get; set; } = PaymentChannel.Wallet;

        public PaidBy PaidFromAccount { get; set; } = PaidBy.Subscriber;
        public string? Reference { get; set; }
    
        public string? WalletPin { get; set; }
        public string? ExternalReference { get; set; }

        public string? PaidByUserId { get; set; }
        public long WalletProviderId { get; set; }
    }
    public class RepaymentReminderDto
    {
        public long SubscriptionId { get; set; }
        public string SubscriberUserId { get; set; }

        public decimal TotalRepayment { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal CreditBalance { get; set; }
        public decimal OutstandingAmount { get; set; }

        public bool RequiresReminder { get; set; }

        // True when AmountPaid alone hasn't cleared the balance, but unconsumed
        // credit is enough to cover what remains, the loan isn't formally closed
        // until someone runs reconciliation to consume that credit.
        public bool RequiresReconciliation { get; set; }

        public string? Message { get; set; }

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public OrganizationDto Organization { get; set; }
    }
    public class RepaymentLedgerEntryDto
    {
        public DateTimeOffset Date { get; set; }
        public string Type { get; set; }       // "Cash", "Produce", or "Hybrid"
        public string Channel { get; set; }    // e.g. "Wallet (Momo)", "Maize", "Wallet (Momo) + Maize"
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Reference { get; set; }
    }
    public class RepaymentScheduleItemDto
    {
        public long ScheduleId { get; set; }
        public int InstallmentNumber { get; set; }
        public DateTimeOffset DueDate { get; set; }

        public decimal ExpectedAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal OutstandingAmount { get; set; }

        public decimal PenaltyAccrued { get; set; }
        public decimal PenaltyPaid { get; set; }
        public decimal PenaltyOutstanding { get; set; }

        public bool IsWithinGracePeriod { get; set; }

        public RepaymentScheduleStatus Status { get; set; }
        public bool RequiresReminder { get; set; }
    }
    public class OverdueRepaymentReminderDto
    {
        public long SubscriptionId { get; set; }
        public string SubscriberUserId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string PackageName { get; set; }

        public RepaymentScheduleItemDto Schedule { get; set; }
    }
    public class CommitmentReminderDto
    {
        public long SubscriptionId { get; set; }
        public string SubscriberUserId { get; set; }
        public decimal TotalCommitment { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal OutstandingAmount { get; set; }
        public string Message { get; set; }
        public bool RequiresReminder { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public OrganizationDto Organization { get; set; }
    }


    public class RecordRepaymentRequest
    {
        public long SubscriptionId { get; set; }

        public decimal Amount { get; set; }

        public string? Reference { get; set; }

        public string? ExternalReference { get; set; }

        public PaymentChannel PaymentChannel { get; set; }
        public string? SubscriberUserId { get; set; }
        public long? WalletProviderId { get; set; }

        // Only populated/required when PaymentChannel == PaymentChannel.Produce
        public ProduceRepaymentDetails? ProduceDetails { get; set; }
    }

    public class ProduceRepaymentDetails
    {
        public long OrganizationProductId { get; set; }
        public long LocationId { get; set; }
        public decimal QuantityBrought { get; set; }
        public string? Grade { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
    public class RepaymentResponse
    {
        public bool IsSuccessful { get; set; }
        public decimal AmountApplied { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal OverpaymentCredited { get; set; }
        public string ExternalReference { get; set; }
    }

    public class ProduceRepaymentResult
    {
        public bool IsSuccessful { get; set; }
        public decimal EquivalentAmount { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal OverpaymentAmount { get; set; }
        public string Reference { get; set; }
    }

    public class SubscriptionQueryFilter
    {
        public long? PackageId { get; set; }
        public string? UserId { get; set; }

        [EnumDataType(typeof(SubscriptionStatus))]
        public SubscriptionStatus? Status { get; set; }

        [EnumDataType(typeof(SubscriptionView))]
        public SubscriptionView? ViewType { get; set; }
        public bool? HasPaidCommitment { get; set; }
        public bool? IsDisbursed { get; set; }
        public bool? IsActiveOnly { get; set; }
        public string? SubscriberUserId { get; set; }
        public string? BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public string? CurrentLoggedInUserRole { get; set; }
        public long? OrganizationId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public long SubscriptionId { get; set; }
        public string? Env { get; set; }
    }


    public class InputObligationDto
    {
        public DateTimeOffset? Date { get; set; }

        public decimal TotalCost { get; set; }

        public decimal AmountPaid { get; set; }

        public decimal RemainingBalance { get; set; }

        public string Name { get; set; }

        public decimal RepaymentProgress { get; set; }
    }
    public class RoleBasedSubscriptionOverviewDto
    {
        public long TotalSubscriptions { get; set; }
        public long PendingApproval { get; set; }
        public long PendingCommitment { get; set; }
        public long InputsReleased { get; set; }
        public long Active { get; set; }
    }
    public class FarmerSubscriptionOverviewDto : RoleBasedSubscriptionOverviewDto
    {
       public long ActivePackages { get; set; }
        public decimal TotalOutstanding { get; set; }
        public string NextDueDate { get; set; }
        public decimal TotalRepaid { get; set; }
    }
    public class InputFinancingDashboardDto
    {
        public decimal TotalFinancedValue { get; set; }
        public decimal TotalAmountRepaid { get; set; }
        public decimal TotalCommitted { get; set; }
        public decimal TotalOutstandingBalance { get; set; }
        public decimal RecoveryRate { get; set; }
        public int TotalActivePackages { get; set; }
    }
    public class SubscriptionDetailDto
    {
        public DateTimeOffset? DisbursedOn { get; set; }
        public DateTimeOffset? NextDueDate { get; set; }
        public string Season { get; set; }
        public decimal ROI { get; set; }
        public decimal TotalRepaymentValue { get; set; }
        public decimal RepaymentBalance { get; set; }
        public string RepaymentMethod { get; set; }
        public decimal CommitmentBalance { get; set; }
        public long SubscriptionId { get; set; }
        public long PackageId { get; set; }
        public string SubscriberName { get; set; }
        public string RiskLevel { get; set; }
        public string SubscriberUserId { get; set; }
        public InputSubScriptionType SubscriptionType { get; set; }
        public string SubscriptionTypeName { get; set; }
        public string CreatedBy { get; set; }
        public string Location { get; set; }
        public string PackageName { get; set; }
        public string RepaymentDuration { get; set; }
        public decimal CommitmentValue { get; set; }
        public decimal TotalFinancingValue { get; set; }
        public DateTimeOffset? SubscribedOn { get; set; }
        public DateTimeOffset? RejectedOn { get; set; }
        public DateTimeOffset? ApprovedOn { get; set; }
        public DateTimeOffset? CommitmentPaymentDeadline { get; set; }

        public string CommitmentStatus { get; set; }
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public string SubscriptionStatusName { get; set; }
        public decimal CommitmentPaid { get; set; }
        // optional sections
        public CommitmentInfo Commitment { get; set; }
        public RepaymentInfo Repayment { get; set; }
    }
    public class CommitmentInfo
    {
        public decimal CommitmentPaid { get; set; }
        public decimal CommitmentBalance { get; set; }
        public int CommitmentDaysLeft { get; set; }
    }
    public class RepaymentInfo
    {
        public decimal TotalRepaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal RecoveryRate { get; set; }
        public DateTimeOffset? NextRepaymentDueDate { get; set; }
        public RiskCategory RiskLevel { get; set; }
        public int DaysOverdue { get; set; }
        public DateTimeOffset? CompletedOn { get; set; }
    }

    public class SubscriptionFullDetailDto
    {
        public SubscriberInfoDto Subscriber { get; set; }
        public PackageInfoDto Package { get; set; }
    }

    public class SubscriberInfoDto
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Type { get; set; }
        public string Agent { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string VerificationStatus { get; set; }
        public DateTimeOffset EnrollmentDate { get; set; }
        public string Package { get; set; }
        public string WarehouseAssigned { get; set; }
    }

    public class PackageInfoDto
    {
        public string PackageName { get; set; }
        public decimal TotalValue { get; set; }
        public bool CommitmentRequired { get; set; }
        public decimal CommitmentValue { get; set; }
        public string RepaymentDuration { get; set; }
        public string RepaymentModel { get; set; }
        public string RiskCategory { get; set; }
        public string PackageTarget { get; set; }
        public string Season { get; set; }
        public decimal RepaymentValue { get; set; }
        public string CommitmentPlan { get; set; }
        public DateTimeOffset? RepaymentStartDate { get; set; }
        public DateTimeOffset? RepaymentEndDate { get; set; }
    }

    public class SubscriptionDocument
    {
        public long Id { get; set; }
        public long SubscriptionId { get; set; }

        public DocumentType Type { get; set; } // CommissionLetter

        public string FileName { get; set; }
        public string FilePath { get; set; }   // or BlobUrl
        public string Key { get; set; }
        public DateTimeOffset UploadedOn { get; set; }
    }

    public class ApproveDisbursementRequestDto
    {
        public long? RequestId { get; set; }
        public long? SubscriptionId { get; set; }
        public string? Reference { get; set; }
        public decimal? Amount { get; set; }


        [Required]
        [EnumDataType(typeof(ApprovalDecision))]
        public ApprovalDecision ApprovalState { get; set; }
    }
    public class PaymentReceiptResponse
    {
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "application/pdf";
        public byte[] FileContent { get; set; } = [];
    }
    public class CommitmentFormDto
    {
        public string FileName { get; set;}
        public string ContentType { get; set;}
        public byte[] FileContent { get; set; }
    }
    public class SubscriptionReviewResponseDto
    {
        public long SubscriptionId { get; set; }
        public List<InputBreakdownDto> InputBreakdown { get; set; }
        public List<AdditionalCostDto> AdditionalCost { get; set; }
        public SubscriberInfoDto Subscriber { get; set; }

        public PackageInfoDto Package { get; set; }

        public FinancingSnapshotDto FinancingSnapshot { get; set; }

        public List<RepaymentHistoryDto> RepaymentHistory { get; set; }
        public List<CommitmentHistoryDto> CommitmentPaymentHistory { get; set; }
    }

    public class FinancingSnapshotDto
    {
        public decimal TotalFinancedValue { get; set; }

        public decimal CommitmentPaid { get; set; }

        public decimal CommitmentAmountRequired { get; set; }

        public decimal CommitmentOutstandingBalance { get; set; }
        public bool RepaymentRequired { get; set; }
        public bool CommitmentRequired { get; set; }
        public string? CommitmentDaysLeft { get; set; }

        public decimal TotalRepaidAmount { get; set; }

        public decimal TotalRepaymentRequired { get; set; }

        public decimal TotalRepaymentBalance { get; set; }

        public string? RepaymentDaysLeft { get; set; }
        public string RepaymentDuration { get; set; }
        public string RepaymentModel { get; set; }
        public string RiskCategory { get; set; }
    }

    public class RepaymentHistoryDto
    {
        public DateTimeOffset? PaymentDate { get; set; }

        public decimal AmountPaid { get; set; }

        public string PaymentMethod { get; set; }

        public string RecordedBy { get; set; }
    }
    public class CommitmentHistoryDto
    {
        public DateTimeOffset? PaymentDate { get; set; }

        public decimal AmountPaid { get; set; }

        public string PaymentMethod { get; set; }

        public string RecordedBy { get; set; }
    }

    public class WalletTransferResponse
    {
        public bool IsSuccessful { get; set; }

        public string Message { get; set; }

        public string ExternalReference { get; set; }

        public decimal Amount { get; set; }

        public string ProviderTransactionId { get; set; }
    }
    
    public class PaymentInitiationResult
    {
        public bool RequiresRedirect { get; set; }
        public string RedirectUrl { get; set; }
        public bool RequiresManualAction { get; set; }
        public string Reference { get; set; }
        public string Message { get; set; }
    }
    public class CreateCommitmentPaymentRequest
    {
        public PaymentType PaymentType { get; set; }
        public long SubscriptionId { get; set; }
        public decimal Amount { get; set; }
        public PaymentChannel PaymentChannel { get; set; }
        public string? Reference { get; set; }
        public string? ExternalReference { get; set; }
        public string? RecordedByUserId { get; set; }
        public long WalletProviderId { get; set; }
    }
    public class PaymentConfirmationRequest
    {
        public TransactionStatus Status { get; set; }

        public string ExternalReference { get; set; }

        public string ResponsePayload { get; set; }
    }

    public class AddPackageItemRequest
    {
        public long OrganizationProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string UnitOfMeasure { get; set; }
    }
    public class UpdateAdditionalCostRequest
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Amount { get; set; }

        public bool IsPerUnit { get; set; }

        public decimal Quantity { get; set; }

        public AdditionalCostType Type { get; set; }
    }
    public class AddAdditionalCostRequest
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Amount { get; set; }

        public bool IsPerUnit { get; set; }

        public decimal Quantity { get; set; }

        public AdditionalCostType Type { get; set; }
    }
    public class UpdateAdditionalCostArgs
    {
        public string? OrganizationCode { get; set; }
        public long OrganizationId { get; set; }
        public long PackageId { get; set; }
        public string BusinessCode { get; set; }
        public long AdditionalCostId { get; set; }
    }
    public class ProviderDebitResult
    {
        public string? ProviderReference { get; set; }
        public string Message { get; set; } = "";
    }

    #region disbursement
    public class DisburseSubscriptionRequest
    {
        public decimal? DisbursedAmount { get; set; }

        public long WarehouseId { get; set; }   
        public string? Reference { get; set; }
        public string? NIN { get; set; }

        /// <summary>Base64-encoded live photo captured on the frontend at disbursement time.</summary>
        public IFormFile UserImage { get; set; }

        /// <summary>Device GPS coordinates sent by the frontend.</summary>
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class DisburseSubscriptionResult
    {
        public bool Success { get; set; }
        public decimal DisbursedAmount { get; set; }
        public bool IsPartial { get; set; }
        public string LocationName { get; set; }
        public List<DisbursementLineItemModel> LineItems { get; set; } = new();
    }
    public class DisbursementLineItemModel
    {
           public long Id { get; set; }
        public long DisbursementId { get; set; }

            public long SubscriptionId { get; set; }
        public long OrganizationProductId { get; set; }
        public decimal SubscribedQuantity { get; set; }
        public decimal DisbursedQuantity { get; set; }
        public decimal PendingQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalDisbursedValue { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public long LocationId { get; set; }
        public string AgentUserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>Per-product tracking row for physical disbursements.</summary>

    /// <summary>Maps an agent user to the location they are permitted to disburse from.</summary>
    public class AgentLocationAssignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid LocationId { get; set; }
        public bool IsActive { get; set; }
     //   public Location Location { get; set; }
    }

    /// <summary>Outbox event payload — extended with geo and partial fields.</summary>
    

    /// <summary>Strongly-typed disbursement exception — caught at controller level
    /// to return 400/422 instead of 500.</summary>
    public class DisbursementException : Exception
    {
        public DisbursementException(string message) : base(message) { }
    }

    #endregion disbursement
}
