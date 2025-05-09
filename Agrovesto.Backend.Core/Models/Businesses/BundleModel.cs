using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class BundleModel
    {
        public bool IsPercentageEquity { get; set; }
        public double EquityPercentage { get; set; }

        public decimal BundleEquityPrice { get; set; }
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<BundleItemsModel>? BundlePackage { get; set; }
        public List<OtherBundlePaymentCostModel>? OtherBundlePaymentCosts { get; set; }
        public long TotalBundlePackagePacked { get; set; }
        public long TotalBundlePackageRequested { get; set; }
        public long TotalBundlePackageDisbursed { get; set; }
        public decimal BundlePrice { get; set; }
        public long AvailableBundlePackagePacked { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string? BelongToBusinessCode { get; set; }

        public bool ShouldAddCommission { get; set; }
        public decimal CommissionPrice { get; set; }
        public decimal BundleEquityPlusCommission { get; set; }
        public long WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public DateTimeOffset? MaturityDate { get; set; }
    }
    public class OtherBundlePaymentCostModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public long BundleId { get; set; }
    }

    public class BundleItemsModel
    {
     
        public long Id { get; set; }
        public long BundleId { get; set; }
        public string? Name { get; set; } //is the warehouse inventory item name same as the bundle item selected
        public long Unit { get; set; }
        public decimal Price { get; set; }
        public long WarehouseInputInventoryId { get; set; } //i. bunlde item must exist in the warehouse
    }

    //when creating a bunlde, foreach unit in bundle items, remove from the warehouseinventory the 
}
