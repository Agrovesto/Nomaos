using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class InputDeliveryTrackerModel
    {
        public long Id { get; set; }
        public long WarehouseId { get; set; }
        public long WarehouseInputInventoryId { get; set; }
        public InputTransitStatus Status { get; set; }// picked up, in transit, arrived, delivered, confirmed
        public string? InputName { get; set; }
        public long QuantityLoaded { get; set; }
        public long QuantityDelivered { get; set; }
        public string? Email { get; set; }
        public long QuantityReceived { get; set; }
        public string? DeliveredTo { get; set; }
        public string? DeliveredBy { get; set; } //delivery guy name
        public string? Reference { get; set; }
        public string? TransitStatus { get; set; }
        public string? DeliveryContact { get; set; }
        public string? PickupLocation { get; set; }
        public string? DeliveryLocation { get; set; }
        public LogisticType LogisticType { get; set; } //truck, bike, cab
        public string? LogisticNumber { get; set; }
        public DateTimeOffset PickUpDate { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? DeliveryDate { get; set; }

        public string? BusinessCode { get; set; }
    }
}
