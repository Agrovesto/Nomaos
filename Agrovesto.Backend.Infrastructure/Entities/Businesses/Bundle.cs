using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class BundleSubscriptionPerFarmer
    {
        public long Id { get; set; }
        public long BundlePackageId { get; set; }
        public long FarmerId { get; set; }
        public long RequestedQuantity { get; set; }
        public long ReceivedQuantity { get; set; }
        public long DisbursedQuantity { get; set; }

    }

    public class BundlePackage
    {
        public string Name { get; set; }
        public long Id { get; set; }
        public string Description { get; set; }
        public decimal BundlePackageCost { get; set; }
    }

    public class BundleInventory
    {
        public long Id { get; set; }
        public string BundleState { get; set; }
        public string BundleLga { get; set; }
        public string BundleLocation { get; set; }
        public long BundlePackageId { get; set; }
        public string BundleSentFrom { get; set; }
        public long TotalBundlePackageForBundleIdReceived { get; set; } //total received for each package
        public long TotalBundlePackageForBundleIdSent { get; set; } //sent from head office
        public long TotalBundlePackageForBundleIdDelivered { get; set; } //delivered by who the company sent
        public long TotalBundlePackageForBundleIdRequestedByFarmer { get; set; }
        public long TotalBundlePackageForBundleIdDisbursedToFarmer { get; set; }
    }

}
