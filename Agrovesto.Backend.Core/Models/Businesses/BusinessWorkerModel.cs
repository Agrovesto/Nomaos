using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class BundleSubscriptionSummary
    {
        public string BundleName { get; set; }  // e.g., Basic Bundle A, Advanced Bundle
        public int TotalSubscribers { get; set; }  // Number of farmers subscribed to the bundle
    }
    public class SubscriptionMetrics
    {
        public int TotalActiveSubscribers { get; set; }
        public int PendingSubscribers { get; set; }
        public int PartialPaymentSubscribers { get; set; }
    }
    public class WarehouseInputTransitModel
    {
        public string TrackingId { get; set; }
        public string Status { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTimeOffset? DateShipped { get; set; }
        public DateTimeOffset? DateReceived { get; set; }
    }
    public class BusinessEntitiesMetrics
    {
        public long TotalWarehouse { get; set; }
        public long TotalAgents { get; set; }
        public long TotalCooperative { get; set; }
        public long TotalFarmer { get; set; }
        public double PercentageFarmerPerformance { get; set; }
        public double PercentageCooperativePerformance { get; set; }
        public double PercentageAgentPerformance { get; set; }
        public double PercentageWarehousePerformance { get; set; }

    }
    public class WarehouseSummary
    {
        public string InputName { get; set; }
        public long InitialStockUp { get; set; }
        public long Stocklevel { get; set; }
     
    }
    public class WarehouseSummaryMetrics
    {
        public string WarehouseName { get; set; }
        public string Location { get; set; }
        public List<WarehouseSummary> StockSummary { get; set; }
    }

    public class BundleSummaryMetrics
    {
        public long ActiveSubscribers { get; set; } //those that paid
        public long PendingSubscription { get; set; } //those yet to pay
        public long OverdueSubscribers { get; set; }
    }
    public class BusinessWorkerModel
    {
        public long BusinessWorkerId { get; set; }
        public string? UserId { get; set; }

        public long BusinessId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
    }
}
