using Agrovesto.Backend.Core.CoreUtils;
using System;
using System.Collections.Generic;
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


    public class CreateInputPackageRequest
    {
        public long PaybackWalletId { get; set; } // wallet type
        public long UserWalletId { get; set; }
        public  decimal TotalPackageValue { get; set; }
        public decimal RepaymentValue { get; set; }
        public string Name { get; set; }
        public long OrganizationId { get; set; }
        public long InputFinancingCategoryId { get; set; }
        public InputSubScriptionType SubScriptionType { get; set; }
        public InputPackageStructure Structure { get; set; }

        public List<CreateInputPackageItemRequest> InputPackageItems { get; set; }

        public List<CreateAdditionalCostRequest> AdditionalCosts { get; set; }

        public InputPackageRepaymentMode RepaymentMode { get; set; }
        public int RepaymentPeriodInMonths { get; set; }

        public bool CommitmentRequired { get; set; }
        public CommitmentType CommitmentType { get; set; }
        public decimal CommitmentValue { get; set; }

        public bool ApplyInterest { get; set; }
        public decimal InterestRate { get; set; }

        public string Currency { get; set; } = "NGN";
    }
    public class CreateInputPackageItemRequest
    {
        public long OrganizationProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string UnitOfMeasure { get; set; }
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
        public long Id { get; set; }
        public string Name { get; set; }

        public decimal TotalPackageValue { get; set; }
        public decimal RepaymentValue { get; set; }

        public InputPackageStatus Status { get; set; }

        public bool IsGeneratedFromEop { get; set; }

        public string Currency { get; set; }

        public List<InputPackageItemDto> Items { get; set; }
        public List<AdditionalCostDto> AdditionalCosts { get; set; }
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
        public int TotalCount { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
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
}
