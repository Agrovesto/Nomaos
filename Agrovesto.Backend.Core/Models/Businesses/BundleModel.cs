using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class BundleModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<BundleItemsModel> BundlePackage { get; set; }
        public long TotalBundlePackagePacked { get; set; }
        public long TotalBundlePackageRequested { get; set; }
        public long TotalBundlePackageDisbursed { get; set; }
        public decimal BundlePrice { get; set; }
        public long AvailableBundlePackagePacked { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string BelongToBusinessCode { get; set; }

    }

    public class BundleItemsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long BundleId { get; set; }
        public string Name { get; set; } //is the warehouse inventory item name same as the bundle item selected
        public long Unit { get; set; }
        public long WarehouseInputInventoryId { get; set; } //i. bunlde item must exist in the warehouse
    }

    //when creating a bunlde, foreach unit in bundle items, remove from the warehouseinventory the 
}
