using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class Bundle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<BundleItems> BundlePackage { get; set; }
        public long TotalBundlePackagePacked { get; set; }
        public long AvailableBundlePackagePacked { get; set; }
        public long TotalBundlePackageRequested { get; set; } //requested bundle must not be more than available bundle quantity
        public long TotalBundlePackageDisbursed { get; set; }
        public decimal BundlePrice { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string BelongToBusinessCode { get; set; }
        //add bundle equity fee
        //add optional commission fee to bundle equity fee
        public bool ShouldAddCommission { get; set; }
        public decimal CommissionPrice { get; set; }
        public decimal BundleEquityPlusCommission { get; set; }

        public decimal BundleEquityPrice { get; set; }
    }

    public class BundleItems
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; } //is the warehouse inventory item name same as the bundle item selected
        public long Unit { get; set; }
        public long WarehouseInputInventoryId { get; set; } //i. bunlde item must exist in the warehouse
        public long BundleId { get; set; }
        public virtual Bundle Bundle { get; set; }
    }

    //when creating a bunlde, foreach unit in bundle items, remove from the warehouseinventory the 
}
