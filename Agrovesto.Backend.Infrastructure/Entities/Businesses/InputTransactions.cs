using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{

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
        public string UnitOfMeasure { get; set; } // e.g., Bag, Kg, Litre
        public decimal UnitCost { get; set; }
        public decimal? OverrideCost { get; set; }
        public bool IsActive { get; set; } = true;
    }
  
    public class InputPackage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long InputFinancingCategoryId { get; set; }
        public InputFinancingCategory InputFinancingCategory { get; set; }
        public InputSubScriptionType SubScriptionType { get; set; }
        public string Name { get; set; } // e.g., Maize wet season
        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public InputPackageStructure PackageStructure { get; set; }
        public List<InputPackageItem> InputPackageItems { get; set; }
        public decimal TotalPackageValue { get; set; } // if package structure is physical, total would be the sum of the cost, if it is monetary, total is the amount entered
       public bool CommitmentRequired { get; set; } // if true, farmer must commit to repay before receiving the package
        public CommitmentType CommitmentType { get; set; } // if commitment required, how is the commitment calculated
        public decimal CommitmentValue { get; set; } // value of the commitment, either a static fee or percentage
        public List<AdditionalInputPackageCost> AdditionalCosts { get; set; }
        public RiskCategory RiskCategory { get; set; } // Low, Medium, High
        public InputPackageRepaymentMode RepaymentMode { get; set; } // Low, Medium, High
       
        public int RepaymentPeriodInMonths { get; set; } //how long does the farmer have to repay, in months
        public long UserWalletId { get; set; } // if package structure is monetary, this is the wallet the money will be disbursed to
        public UserWallet UserWallet { get; set; } //wallet tied to organization or business, not individual farmer wallet
        public decimal RepaymentValue { get; set; } // amount farmer has to repay, if repayment mode is cash or hybrid, this is the cash amount, if repayment mode is produce, this is the value of the produce to be repaid
        public string? Season { get; set; } // e.g. "Wet Season 2024"
        public InputPackageStatus Status { get; set; }
        public bool IsActive { get; set; } = true;
        public int Version { get; set; }
        public long? ParentPackageId { get; set; }
        public InputPackage ParentPackage { get; set; }
        public List<InputPackage> ChildVersions { get; set; }
        public bool AllowPartialUtilization { get; set; }
        public DateTimeOffset? PlannedDisbursementDate { get; set; }
        public decimal InterestRatePercentage { get; set; }
        public bool IsInterestApplied { get; set; } = false;
        public RepaymentScheduleType ScheduleType { get; set; }
       
        public string Currency { get; set; } = "NGN";
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? UpdatedOn { get; set; }
        public long CreatedByUserId { get; set; }
        public long? EconomicsOfProductionId { get; set; }
        public EconomicsOfProduction EconomicsOfProduction { get; set; }
        public bool IsGeneratedFromEop { get; set; } =false;
        public bool IsEditable { get; set; } = true;
        public DateTimeOffset? PublishedOn { get; set; }
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
    public class FarmerInputSubscription
    {
        [Key]
        public long Id { get; set; }

        public long FarmerId { get; set; }
        public Farmer Farmer { get; set; }

        public long InputPackageId { get; set; }
        public InputPackage InputPackage { get; set; }

        public string PackageNameSnapshot { get; set; }
        public decimal TotalPackageValueSnapshot { get; set; }

        public decimal CommitmentAmount { get; set; }
        public bool CommitmentPaid { get; set; }

        public decimal RepaymentAmount { get; set; }

        public InputPackageRepaymentMode RepaymentMode { get; set; }

        public SubscriptionStatus Status { get; set; }

        public DateTime SubscribedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public long OrganizationId { get; set; }

        public List<SubscribedInputItem> Items { get; set; }
        public List<InputDisbursement> Disbursements { get; set; }
        public List<RepaymentTransaction> Repayments { get; set; }
    }

    public class SubscribedInputItem
    {
        [Key]
        public long Id { get; set; }

        public long FarmerInputSubscriptionId { get; set; }
        public FarmerInputSubscription Subscription { get; set; }

        public long OrganizationProductId { get; set; }

        public string ProductName { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }

        public decimal TotalCost => Quantity * UnitCost;
    }

    public class InputDisbursement
    {
        [Key]
        public long Id { get; set; }

        public long FarmerInputSubscriptionId { get; set; }
        public FarmerInputSubscription Subscription { get; set; }

        public DisbursementType Type { get; set; }

        public decimal Amount { get; set; }

        public long? WalletId { get; set; }
        public UserWallet Wallet { get; set; }

        public DisbursementStatus Status { get; set; }

        public DateTime DisbursedAt { get; set; }

        public string Reference { get; set; } // payment ref or logistics ref

        public List<DisbursedItem> Items { get; set; } // for physical inputs
    }
    public class DisbursedItem
    {
        [Key]
        public long Id { get; set; }

        public long InputDisbursementId { get; set; }
        public InputDisbursement Disbursement { get; set; }

        public long OrganizationProductId { get; set; }

        public decimal Quantity { get; set; }
    }

    public class RepaymentTransaction
    {
        [Key]
        public long Id { get; set; }

        public long FarmerInputSubscriptionId { get; set; }
        public FarmerInputSubscription Subscription { get; set; }

        public RepaymentType Type { get; set; }

        public decimal Amount { get; set; }

        public decimal? Quantity { get; set; } // for produce repayment
        public long? OrganizationProductId { get; set; } // produce type

        public string Reference { get; set; }

        public DateTime TransactionDate { get; set; }

        public PaymentChannel Channel { get; set; }

        public TransactionStatus Status { get; set; }
    }
 
    public class InputAllocation
    {
        public long Id { get; set; }

        public long FarmerId { get; set; }
        public Farmer Farmer { get; set; }

        public long FarmId { get; set; }
        public Farm Farm { get; set; }

        public long InputId { get; set; }
        public InputPackage Input { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal TotalCost => Quantity * UnitPrice;

        public DateTime AllocatedDate { get; set; }

        public long? InputTransactionId { get; set; }
        public InputTransaction InputTransaction { get; set; }
    }
    public class InputTransaction
    {
        public long Id { get; set; }

        // Relationship
        public long FarmerId { get; set; }
        public Farmer Farmer { get; set; }

        public long? FarmId { get; set; }  // Optional (if tied to a specific farm)
        public Farm Farm { get; set; }

        // Financial
        public decimal Amount { get; set; }

        public InputTransactionType TransactionType { get; set; }

        public PaymentStatus PaymentStatus { get; set; }  // For repayment tracking

        // Optional metadata
        public string ReferenceNumber { get; set; }
        public string Description { get; set; }

        // Audit
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }

        public ICollection<InputAllocation> InputAllocations { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
    public enum InputTransactionType
    {
        Disbursement = 1,  // Inputs given to farmer
        Repayment = 2      // Farmer paying back
    }
   
}
