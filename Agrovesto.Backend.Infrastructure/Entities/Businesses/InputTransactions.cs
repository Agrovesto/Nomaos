using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class Input
    {
        public long Id { get; set; }

        public string Name { get; set; }              // e.g., NPK Fertilizer
        public string Category { get; set; }          // Fertilizer, Seeds, Chemicals
        public string UnitOfMeasure { get; set; }     // Bag, Kg, Litre

        public decimal UnitPrice { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<InputAllocation> InputAllocations { get; set; }
    }
    public class InputAllocation
    {
        public long Id { get; set; }

        public long FarmerId { get; set; }
        public Farmer Farmer { get; set; }

        public long FarmId { get; set; }
        public Farm Farm { get; set; }

        public long InputId { get; set; }
        public Input Input { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal TotalCost => Quantity * UnitPrice;

        public DateTime AllocatedDate { get; set; }

        public long? InputTransactionId { get; set; }
        public InputTransaction InputTransaction { get; set; }
    }
    public class InputTransaction
    {
        public long Id { get; set; }

        // Relationship
        public long FarmerId { get; set; }
        public Farmer Farmer { get; set; }

        public long? FarmId { get; set; }  // Optional (if tied to a specific farm)
        public Farm Farm { get; set; }

        // Financial
        public decimal Amount { get; set; }

        public InputTransactionType TransactionType { get; set; }

        public PaymentStatus PaymentStatus { get; set; }  // For repayment tracking

        // Optional metadata
        public string ReferenceNumber { get; set; }
        public string Description { get; set; }

        // Audit
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }

        public ICollection<InputAllocation> InputAllocations { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
    public enum InputTransactionType
    {
        Disbursement = 1,  // Inputs given to farmer
        Repayment = 2      // Farmer paying back
    }
    public enum PaymentStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3
    }
}
