using Agrovesto.Backend.Core.CoreUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public InputPackageRepaymentMode RepaymentMode { get; set; }
        public decimal RepaymentValue { get; set; }
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
        public AdditionalCostType Type { get; set; }
        public bool IsPerUnit { get; set; } // e.g. per hectare
        public decimal? Quantity { get; set; }
    }


    public class UpdateInputPackageConfigRequest
    {
        public DateTimeOffset CommitmentPaymentStartDate { get; set; }
        public DateTimeOffset CommitmentPaymentEndDate { get; set; }

        public RepaymentFrequency RepaymentFrequency { get; set; }
        public int RepaymentDuration { get; set; }

        public DateTimeOffset RepaymentStartDate { get; set; }
        public DateTimeOffset RepaymentEndDate { get; set; }

        public DateTimeOffset RepaymentGracePeriod { get; set; }
    }
    //views
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
    }
    public class InputPackageFinancingStructureDto
    {
        public decimal TotalFinancingValue { get; set; }
        public bool CommitmentRequired { get; set; }
        public decimal CommitmentValue { get; set; }
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
    public class  InputPackageRepaymentWallet
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
        public string Name { get; set; }
        public decimal Amount { get; set; }
    }
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
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
    }
    public class ReplacePackageItemRequestArgs
    {
        public long PackageId { get; set; }

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
        public long PackageId { get; set; }
        public RepaymentFrequency Frequency { get; set; }
        public int Duration { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public DateTimeOffset GracePeriod { get; set; }

        public decimal TotalAmount { get; set; }
    }
}
