using Agrovesto.Backend.Core.Enums;
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


    public class InventoryTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string? Grade { get; set; }
        public decimal UnitPriceUsed { get; set; }
        // Inventory affected
        public long LocationInventoryId { get; set; }
        public LocationInventory LocationInventory { get; set; }

        // Product
        public long? OrganizationProductId { get; set; }
        public OrganizationProduct OrganizationProduct { get; set; }

        // Source locations
        public long? FromLocationId { get; set; }
        public OrganizationLocation? FromLocation { get; set; }
        //destination location
        public long? ToLocationId { get; set; }
        public OrganizationLocation? ToLocation { get; set; }
        public string? Notes { get; set; }
        // IN or OUT
        public InventoryTransactionType TransactionType { get; set; }
        // STOCK_IN
        // STOCK_OUT
        // TRANSFER
        // ADJUSTMENT
        // DAMAGE
        // RETURN
        // SALES

        // Quantity moved
        public decimal Quantity { get; set; }

        public string QuantityType { get; set; } = "Bag";

        // Before & after balances
        public decimal QuantityBefore { get; set; }
        public decimal QuantityAfter { get; set; }

        // Batch tracking
        public string? BatchNumber { get; set; }

        // Why movement happened
        public string? Reason { get; set; }

        // Optional reference
        public string? ReferenceNumber { get; set; }

        // User auditing
        public string PerformedByUserId { get; set; }

        public DateTimeOffset TransactionDate { get; set; }
            = DateTimeOffset.Now;

        public string BusinessCode { get; set; }

        // Optional status
        public string Status { get; set; } = "Completed";
        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public long? SubscriptionId { get; set; }

        public double Latitude { get; set; } 
        public double Longitude { get; set; }
        public decimal Amount { get; set; }
        
    }
    public class Agent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
       
        public string? BelongToBusinessCode { get; set; }  //which business
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string? SupervisorUserId { get; set; }
        public string? OnboardedById { get; set; }
        public bool IsActive { get; set; } = true;
        public Tier TierLevel { get; set; }
        public AgentStatus AgentStatus { get; set; } = AgentStatus.Active;

    }

    public class OrganizationAgent //Agents that belong to organization
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public long AgentId { get; set; }
        public Agent Agent { get; set; }
        public ICollection<OrganizationAgentRole> Roles { get; set; }

        public DateTimeOffset AssignedOn { get; set; } = DateTimeOffset.Now;
        public string? AssignedByUserId { get; set; }
        public long? LocationId { get; set; } //warehouse/aggregration centre, distribution centre etc
       public OrganizationLocation Location { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsPrimary { get; set; }
        public string? DeactivationReason { get; set; }
        public string? DeactivatedByUserId { get; set; }
        public DateTimeOffset? DeactivatedOn { get; set; }
    }
    public class OrganizationAgentRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long OrganizationAgentId { get; set; }
        public OrganizationAgent OrganizationAgent { get; set; }

        public long RoleId { get; set; }
        public OrganizationRole OrganizationRole { get; set; }

        public DateTimeOffset AssignedOn { get; set; } = DateTimeOffset.Now;
    }
    public class OrganizationRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string RoleName { get; set; }
        public string Description { get; set; }

        public string BusinessCode { get; set; }
    }
    public class LocationInventory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long LocationId { get; set; }
        public OrganizationLocation Location { get; set; }
        public DateTimeOffset RecordedOn { get; set; } = DateTimeOffset.Now;
         public long? OrganizationProductId { get; set; }
        public OrganizationProduct OrganizationProduct { get; set; }
        public string? BatchNumber { get; set; } //can be auto generated or provided by user
        public string? InventoryOwner { get; set; } = "Organization"; // Central, Government, Private, Cooperative, organization, farmer
        public decimal Quantity { get; set; }
        public string? QuantityType { get; set; } = "Bag";
         public string? Description { get; set; }
        public DateTimeOffset? StorageDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public string? QualityGrade { get; set; }
        public bool IsActive { get; set; } = true;
        public string RecordByUserId { get; set; }
        public string BusinessCode { get; set; }
        public string? DeletedByUserId { get; set; }
        public DateTimeOffset? DeletedOn { get; set; }
        public string? DeleteReason { get; set; }
        public decimal TotalAmount { get; set; } //only used if inventorytype is money
        public LocationInventoryType LocationInventoryType { get; set; } = LocationInventoryType.Product;
    }
    public enum LocationInventoryType
    {
        Product = 1,
        Monetary = 2,
    }
    public class OrganizationLocation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string LocationName { get; set; }
        public string OwnershipType { get; set; } = "Organization"; // Central, Government, Private, Cooperative, organization, farmer
        public decimal Capacity { get; set; }
        public string? CapacityType { get; set; } = "Bag";
        public decimal StockLevel { get; set; }
        public long LocationTypeId { get; set; }
        public LocationType LocationType { get; set; }
        public long StateId { get; set; }
        public States State { get; set; }
        public long LgaId   { get; set; }
        public LGA LGA { get; set; }

        public long? WardId { get; set; }
        public Ward Ward { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public string BusinessCode { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? DeactivationReason { get; set; }
        public string? DeactivatedByUserId { get; set; }
        public DateTimeOffset? DeletedOn { get; set; }
        public long? ParentLocationId { get; set; }
        public OrganizationLocation? ParentLocation { get; set; }
        public ICollection<OrganizationLocation> SubLocations { get; set; } = new List<OrganizationLocation>();
        public DateTime? DateOfCommissioning { get; set; }
        public string? LocationStatus { get; set; } //= "Active";//Active, Inactive, under construction, suspended
        public bool HasTemperatureControl { get; set; }
        public long? TemperatureControlTypeId { get; set; }
        public TemperatureControlType? TemperatureControlType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Many-to-Many
        public ICollection<LocationModule> LocationModules { get; set; } = new List<LocationModule>();
        public ICollection<LocationStorageCategory> LocationStorageCategories { get; set; } = new List<LocationStorageCategory>();
        public ICollection<OrganizationAgent> OrganizationAgents { get; set; }
    = new List<OrganizationAgent>();
    }
    public class LocationStorageCategory
    {
        public long LocationId { get; set; }
        public OrganizationLocation Location { get; set; }

        public long StorageCategoryId { get; set; }
        public StorageCategory StorageCategory { get; set; }
    }
    public class LocationModule
    {
        public long LocationId { get; set; }
        public OrganizationLocation Location { get; set; }

        public long ModuleId { get; set; }
        public Module Module { get; set; }
    }

    public class LocationType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string? BusinessCode { get; set; }
    }
    public class OrganizationAgentPermission
    {

    }


    public class AgentActivity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long AgentId { get; set; }

        public string OrganizationCode { get; set; }

        public long? LocationId { get; set; }

        public string ActivityType { get; set; }

        public string? Description { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
    }

    public class OrganizationProduct //input types, commodities etc that the organization deals with
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public long OrganizationProductCategoryId { get; set; }
        public OrganizationProductCategory OrganizationProductCategory { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string? Season { get; set; }
        public string BusinessCode { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string? UnitType { get; set; } //kg, bag, litre etc
        public double MinimumOrderQuantity { get; set; }
        public double MaximumOrderQuantity { get; set; }
        public string? Description { get; set; }
        public decimal PricePerUnit { get; set; }

    }
     public class  OrganizationProductCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string CategoryName { get; set; }
        public string BusinessCode { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;

    }
}
