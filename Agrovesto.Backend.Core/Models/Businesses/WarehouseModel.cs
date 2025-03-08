using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class WarehouseModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string Capacity { get; set; }
        public long ManagedByUserId { get; set; }
        public bool HasbeenAssignedManager { get; set; }
        public string? BelongToBusinessCode { get; set; }
    }

    public class WarehouseInputInventoryModel
    {
      
        public long Id { get; set; }
        public string? InputName { get; set; }
        public string? InputDescription { get; set; }
        public string? Type { get; set; }
        public string? Brand { get; set; }
        public decimal Price { get; set; }
        public string? Size { get; set; }
        public string? Reference { get; set; }
        public long UnitAdded { get; set; }
        public long UnitStockedUp { get; set; }
        public long UnitDepleted { get; set; }
        public long UnitBalance { get; set; }
        public bool IsAvailable { get; set; }
        public DateTimeOffset? InventoryStockedOn { get; set; }// = DateTimeOffset.Now;
        public DateTimeOffset? ModifiedOn { get; set; }
        public DateTimeOffset? ShipmentDate { get; set; }
        public string? ManagedByUserId { get; set; }
        public string? BelongToBusinessCode { get; set; }
        public long WarehouseId { get; set; }
        public string? InputComingFrom { get; set; }
        public InputTransitStatus TransitStatus { get; set; }
        public string? DeliveredTo { get; set; }
        public string? ReceivedBy { get; set; }
    }
    public enum InputTransitStatus
    {
        Packed,
        InTransit,
        Received
    }
    public class WarehouseUserModel
    {
      
        public long Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string PhoneNumber { get; set; }
        public string? BelongToBusinessCode { get; set; }
        public string? UserId { get; set; }
        public long WarehouseId { get; set; }
    }

    public class InputUpdateModel
    {
        public long InputId { get; set; }
        public long UnitsToAdd { get; set; }
        public long UnitsToRemove { get; set;}
    }
}
