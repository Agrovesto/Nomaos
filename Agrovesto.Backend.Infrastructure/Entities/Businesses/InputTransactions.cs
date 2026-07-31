using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class OutboxMessage
    {
     
        public Guid Id { get; set; }
        public string EventType { get; set; }
        public string Payload { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ProcessedOn { get; set; }
        public bool IsProcessed { get; set; }
    }
    /**Configuration Layer
InputPackage
InputPackageItem
EOP
2. Commitment Layer
FarmerInputSubscription
3. Execution Layer
InputDisbursement
RepaymentTransaction
     */
    public class InputFinancingCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; } // e.g. Crop, Livestock, Equipment, Mixed
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public string BusinessCode { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        // public ICollection<Input> Inputs { get; set; }
    }
  
    public class InputPackageItem
    {       
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long InputPackageId { get; set; }
        public InputPackage InputPackage { get; set; }
        public long OrganizationProductId { get; set; } //input type Id
        public OrganizationProduct OrganizationProduct { get; set; }
        public decimal Quantity { get; set; }
        public string? UnitOfMeasure { get; set; } // e.g., Bag, Kg, Litre
        public decimal UnitCost { get; set; }
        public decimal? OverrideCost { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class RepaymentPlan
    {
        public long Id { get; set; }
        public long InputPackageId { get; set; }
        public InputPackage InputPackage { get; set; }
        public decimal RepaymentValue { get; set; } // amount farmer has to repay, if repayment mode is cash or hybrid, this is the cash amount, if repayment mode is produce, this is the value of the produce to be repaid
        public RepaymentScheduleType ScheduleType { get; set; }

        public RepaymentFrequency Frequency { get; set; }
        public RepaymentFrequency RepaymentFrequency { get; set; }
        public int RepaymentDuration { get; set; } //how long does the farmer have to repay, in months
        public InputPackageRepaymentMode RepaymentMode { get; set; } // Low, Medium, High
        public RiskCategory RiskCategory { get; set; } // Low, Medium, High
        public DateTimeOffset? RepaymentStartDate { get; set; }
        public DateTimeOffset? RepaymentEndDate { get; set; }
        public DateTimeOffset? RepaymentGracePeriod { get; set; }
        

        public string? PatternType { get; set; }
        // e.g MonthlyOnFirst, WeeklyOnMonday, CustomDates

        public bool AlignToDisbursement { get; set; } = true;

        //default rule
        public decimal InterestRate { get; set; } // optional or required
        public InterestType InterestType { get; set; } // Flat, Daily, Monthly, Compound
        public bool ApplyInterestOnDefault { get; set; }
        public decimal DefaultPenaltyRate { get; set; }
    }
    public class CommitmentPlan
    {
        public long Id { get; set; }
        public long InputPackageId { get; set; }
        public InputPackage InputPackage { get; set; }
        public decimal CommitmentPercentage { get; set; }
        public CommitmentType CommitmentType { get; set; } // if commitment required, how is the commitment calculated
        public decimal CommitmentValue { get; set; } // value of the commitment, either a static fee or percentage
        public DateTimeOffset? CommitmentPaymentStartDate { get; set; }
        public DateTimeOffset? CommitmentPaymentEndDate { get; set; }

    }
    public class InputPackage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; } // e.g., Maize wet season
        public long InputFinancingCategoryId { get; set; }
        public InputFinancingCategory InputFinancingCategory { get; set; }
        public InputSubScriptionType SubScriptionType { get; set; }
        
        public long? OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public string? InputTarget { get; set; } // e.g. crop, livestock, fishery
        public string? InputSeason { get; set; } // e.g. 2024 wet season
        public InputPackageStructure PackageStructure { get; set; }
        public List<InputPackageItem> InputPackageItems { get; set; } = new();
        public decimal TotalPackageValue { get; set; } // if package structure is physical, total would be the sum of the cost, if it is monetary, total is the amount entered
        public bool CommitmentRequired { get; set; } = false;// if true, farmer must commit to repay before receiving the package
        public bool RepaymentRequired { get; set; } = false;
        public long? UserWalletId { get; set; } // if wallet has not been provisioned, it should be null, this allows for package configuration before wallet setup, and also allows for packages that do not require wallet disbursement (e.g. physical input delivery by 3rd party)
        public UserWallet UserWallet { get; set; } //wallet tied to organization or business, not individual farmer wallet

        public long? WalletProviderId { get; set; }
        public WalletProvider WalletProvider { get; set; }

        public bool IsSelectedWalletProvisioned { get; set; } // indicates if the user wallet for disbursement has been provisioned, this is important for package activation and disbursement readiness
        public decimal AdditionalCostAmount { get; set; } // total of all additional costs, this is important for calculating total repayment amount and for farmer commitment
        public List<AdditionalInputPackageCost> AdditionalCosts { get; set; } = new();
        public decimal TotalFinancingValue { get; set; }// => TotalPackageValue + AdditionalCostAmount; // total cost of the package including additional costs, this is important for calculating total repayment amount and for farmer commitment
        public string Currency { get; set; } = "NGN";

       // public InputPackageStatus Status { get; set; }
        public bool IsActive { get; set; } = true;

        public int Version { get; set; }
        public long? ParentPackageId { get; set; }
        public InputPackage ParentPackage { get; set; }
        public List<InputPackage> ChildVersions { get; set; } = new();

        public bool AllowPartialUtilization { get; set; }
        public DateTimeOffset? PlannedDisbursementDate { get; set; }
        public decimal InterestRatePercentage { get; set; }
        public bool IsInterestApplied { get; set; } = false;
       
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? UpdatedOn { get; set; }
        public string CreatedByUserId { get; set; }
        public User CreatedBy { get; set; }
        public string? UpdatedByUserId { get; set; }
        public User UpdatedBy { get; set; }
        public long? EconomicsOfProductionId { get; set; }
        public EconomicsOfProduction EconomicsOfProduction { get; set; }
        public bool IsGeneratedFromEop { get; set; } =false;
        public bool IsEditable { get; set; } = true;
        public DateTimeOffset? PublishedOn { get; set; }
        public string? BusinessCode { get; set; }

        public RepaymentPlan RepaymentPlan { get; set; }

        public CommitmentPlan CommitmentPlan { get; set; }
        public ICollection<InputPackageSubscription> Subscriptions { get; set; }
    = new List<InputPackageSubscription>();

    }
    
    public class AdditionalInputPackageCost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public long InputPackageId { get; set; }
        public InputPackage InputPackage { get; set; }
        public bool IsActive { get; set; } = true;
        public AdditionalCostType Type { get; set; }
        public bool IsPerUnit { get; set; } // e.g. per hectare
        public decimal? Quantity { get; set; }
        public AdditionalCostTreatmentType AdditionalCostTreatmentType { get; set; } = AdditionalCostTreatmentType.Internal;
    }

    // public ICollection<InputAllocation> InputAllocations { get; set; }
    public class EconomicsOfProduction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string Name { get; set; } // e.g. Maize Wet Season - Kaduna
        public string Crop { get; set; }
        public decimal LandSize { get; set; } // e.g. per hectare
        public string UnitOfMeasure { get; set; } = "Hectare";
        public decimal ExpectedYield { get; set; } // e.g. tons per hectare
        public decimal ExpectedMarketPrice { get; set; } // per ton
        public decimal TotalCostOfProduction { get; set; }
        public decimal ExpectedRevenue => ExpectedYield * ExpectedMarketPrice;
        public decimal ExpectedProfit => ExpectedRevenue - TotalCostOfProduction;

        public string Season { get; set; }

        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public List<EopCostItem> CostItems { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedOn { get; set; }

        public string BusinessCode { get; set; }
    }
    public class EopCostItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long EconomicsOfProductionId { get; set; }
        public EconomicsOfProduction EconomicsOfProduction { get; set; }

        public string Name { get; set; } // e.g. Fertilizer, Seeds, Labor
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => Quantity * UnitCost;
        public long? OrganizationProductId { get; set; } // optional link to actual product
        public OrganizationProduct OrganizationProduct { get; set; }
    }
        
    public class RepaymentSchedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
       
        public long InputPackageSubscriptionId { get; set; }
        public InputPackageSubscription InputPackageSubscription { get; set; }
        public int InstallmentNumber { get; set; }

        public DateTimeOffset DueDate { get; set; }

        public decimal Amount { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTimeOffset? PaidOn { get; set; }
      
        public bool IsWithinGracePeriod { get; set; }

        public bool IsPaid { get; set; }
        public RepaymentScheduleStatus Status { get; set; }
        public decimal Balance { get; set; }// => Amount - AmountPaid;
        public bool IsOverdue { get; set; }
        public decimal PenaltyAccrued { get; set; }
        public decimal PenaltyPaid { get; set; }
    }

    public class InputPackageSubscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long InputPackageId { get; set; }
        public InputPackage InputPackage { get; set; }
        public long? OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public string SubscriberUserId { get; set; }
        public User SubscriberUser { get; set; }
        public string AddedByUserId { get; set; }
        public User AddedByUser { get; set; }
        public SubscriptionStatus Status { get; set; }

        public InputSubScriptionType SubscriberType { get; set; } //membership, individual or both
        public DateTimeOffset SubscribedOn { get; set; }
        public bool IsActive { get; set; } = true;
        public RepaymentFrequency RepaymentFrequency { get; set; }

        public int RepaymentDuration { get; set; }

        public DateTimeOffset? RepaymentStartDate { get; set; }

        public DateTimeOffset? RepaymentEndDate { get; set; }

        public DateTimeOffset? RepaymentGracePeriod { get; set; }

        public InputPackageRepaymentMode RepaymentMode { get; set; }

        public decimal TotalFinancingValue { get; set; }

        public bool CommitmentRequired { get; set; }
        public decimal RepaymentValue { get; set; }

        public decimal CommitmentValue { get; set; }

        public DateTimeOffset? CommitmentPaymentStartDate { get; set; }

        public DateTimeOffset? CommitmentPaymentEndDate { get; set; }

        public string? ApprovedByUserId { get; set; }
        public DateTimeOffset? ApprovedOn { get; set; }

        public string? RejectedByUserId { get; set; }
        public DateTimeOffset? RejectedOn { get; set; }

        public string? RejectionReason { get; set; }

        public DateTimeOffset? DisbursedOn { get; set; }
        public DateTimeOffset? CompletedOn { get; set; } //payment completed on
        public RiskCategory RiskCategory { get; set; } = RiskCategory.Low;
        public DateTimeOffset? LastOverdueDetectedOn { get; set; }

        public int OverdueCount { get; set; }

        public RiskCategory RiskLevel { get; set; } = RiskCategory.Low;

        public string BusinessCode { get; set; }
        public ICollection<RepaymentSchedule> RepaymentSchedules { get; set; }
        = new List<RepaymentSchedule>();

        public ICollection<InputPackageSubscriptionPayment> Payments { get; set; }
        =new List<InputPackageSubscriptionPayment>();
    }
    // for auditability — every overpayment event is recorded individually,
    // not just netted into a running total, so you can see where credit came from.
    public class SubscriptionCreditBalance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long SubscriptionId { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public bool IsConsumed { get; set; }
        public DateTimeOffset? ConsumedOn { get; set; }
    }
    public class InputPackageSubscriptionPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long SubscriptionId { get; set; }

        public InputPackageSubscription Subscription { get; set; }

        public PaymentType PaymentType { get; set; } // Commitment

        public decimal AmountPaid { get; set; }

        public PaymentChannel PaymentChannel { get; set; }

        public TransactionStatus Status { get; set; }

        public string? Reference { get; set; }

        public DateTimeOffset? PaidOn { get; set; }
        public string RecordedByUserId { get; set; }
        public User RecordedByUser { get; set; }
        public string? ExternalReference { get; set; }
        public string? TransactionResponse { get; set; }
        public long? WalletProviderId { get; set; }
        public WalletProvider WalletProvider { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }
    
    public class RepaymentPenalty
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long SubscriptionId { get; set; }

        public long RepaymentScheduleId { get; set; }

        public decimal FlatFeeApplied { get; set; }

        public decimal InterestApplied { get; set; }

        public decimal TotalPenalty { get; set; }

        public DateTimeOffset CalculatedOn { get; set; }

        public bool IsReversed { get; set; }
    }
    public class PenaltyPolicy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long InputPackageId { get; set; }

        public decimal FlatFee { get; set; }

        public decimal DailyInterestRate { get; set; } // e.g. 0.5% = 0.005

        public decimal MaxPenaltyCap { get; set; }

        public int GracePeriodDays { get; set; }

        public bool IsActive { get; set; }
    }
    public class RepaymentCollectionCase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long SubscriptionId { get; set; }

        public string AssignedAgent { get; set; }

        public Stage Stage { get; set; }
    }

    public class SubscriptionPaymentAllocation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long InputPackageSubscriptionPaymentId { get; set; }

        public long? RepaymentScheduleId { get; set; }

        public decimal PenaltyPortion { get; set; }

        public decimal InterestPortion { get; set; }

        public decimal PrincipalPortion { get; set; }
    }
    //repayment schedule == repaymentplan + subscription + disbursement
  
public class SubscriptionDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long SubscriptionId { get; set; }
        public InputPackageSubscription Subscription { get; set; }
        public DocumentType DocumentType { get; set; }
        public DateTimeOffset UploadedOn { get; set; }
        public string UploadedBy { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Key { get; set; }
    }

    public class InputSubscriptionDisbursement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long SubscriptionId { get; set; }
        public InputPackageSubscription Subscription { get; set; }
        public decimal? RequestedAmount { get; set; }
        public string? RequestedByUserId { get; set; }
        public User RequestByUser { get; set; }
        public string? Reference { get; set; }
        public DateTimeOffset RequestCreatedOn { get; set; } = DateTimeOffset.UtcNow;
        public string? ApprovedByUserId { get; set; }
        public User ApprovedByUser { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public DateTimeOffset? ApprovedOn { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? DisbursedOn { get; set; }
        public string? DisbursedByUserId { get; set; }
        public User DisbursedByUser { get; set; }
        public decimal? DisbursedAmount { get; set; }
        public decimal? RemainingBalance { get; set; }
        public bool IsPartial { get; set; }
        public long? LocationId { get; set; }
        public OrganizationLocation Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DisbursementStatus Status { get; set; } = DisbursementStatus.PendingApproval;
    }

    public class PaymentReconciliation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long PaymentId { get; set; }
        public InputPackageSubscriptionPayment Payment { get; set; }
        public string ReviewedByUserId { get; set; }
        public bool IsConfirmed { get; set; }
        public string Notes { get; set; }
        public DateTimeOffset ReviewedOn { get; set; }
    }
    public class DisbursementLineItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long DisbursementId { get; set; }

        public InputSubscriptionDisbursement Disbursement { get; set; }


        public long SubscriptionId { get; set; }
        public InputPackageSubscription Subscription { get; set; }
        public long OrganizationProductId { get; set; }
        public OrganizationProduct OrganizationProduct { get; set; }
        public decimal SubscribedQuantity { get; set; }
        public decimal DisbursedQuantity { get; set; }
        public decimal PendingQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalDisbursedValue { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public long LocationId { get; set; }
        public OrganizationLocation OrganizationLocation { get; set; }
        public string AgentUserId { get; set; }
        public User User { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>Ledger row for every monetary disbursement — full or partial.</summary>
    public class MonetaryDisbursementTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long SubscriptionId { get; set; }
        public InputPackageSubscription Subscription { get; set; }

        public long DisbursementId { get; set; }
        public InputSubscriptionDisbursement Disbursement { get; set; }
        public long LocationId { get; set; }
        public OrganizationLocation OrganizationLocation { get; set; }
        public decimal SubscribedAmount { get; set; }
        public decimal DisbursedAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public string ReferenceNumber { get; set; }
        public string AgentUserId { get; set; }
        public User User { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTimeOffset DisbursedOn { get; set; }
    }

}
