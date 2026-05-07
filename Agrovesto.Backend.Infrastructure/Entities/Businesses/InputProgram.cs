using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using Agrovesto.Backend.Core.Models.Businesses.Input;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class SubscriptionRepayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        [Required]
        public string OrganizationCode { get; set; }
        [Required]
        public string BusinessCode { get; set; }
        [Required] 
        public long DistributionsId { get; set; }
        public Distributions Distributions { get; set; }

        // who is paying
        [Required] 
        public string PaidByType { get; set; } // "Farmer" or "Cooperative"
        [Required] 
        public string PaidById { get; set; }

        public RepaymentMode Mode { get; set; } = RepaymentMode.Cash;
        public decimal? AmountPaid { get; set; } // monetary value
        public string ProduceDetails { get; set; } // e.g. "200kg Maize, grade A" or JSON

        public DateTimeOffset PaymentDate { get; set; } = DateTimeOffset.UtcNow;
        public RepaymentStatus Status { get; set; } = RepaymentStatus.Partial;

        // reference to generated receipt
        public Receipt Receipt { get; set; }
    }
    public class Subscriptions
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        [Required]
        public string OrganizationCode { get; set; }
        [Required]
        public string BusinessCode { get; set; }

        public long? RequestingForFarmerId { get; set; } // optional (if farmer-based subscription)
        public long? InitiatedByCooperativeId { get; set; } // optional (if cooperative-based subscription)
        [Required]
        public string BeneficiaryType { get; set; } // "Farmer" or "Cooperative"
        public long BeneficiaryId { get; set; } //id of farmer or cooperative requesting

        public long? BundlePlanId { get; set; } // null if org doesn’t use bundle
        public OrganizationBundlePlans OrganizationBundlePlans { get; set; }

        public string InputDescription { get; set; }
       
        public decimal TotalCost { get; set; }
        public decimal EquityContribution { get; set; } // cash, sweat, land etc.
        public string EquityType { get; set; } // "Cash", "Sweat", "Land", "Mixed"
        public decimal OrganizationContribution { get; set; } = 0;
        public decimal PartnerContribution { get; set; } = 0;
        public DateTimeOffset SubscriptionInitiatedOn { get; set; }
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;// "PendingApproval", "Approved", "Rejected"
        public DateTimeOffset SubscriptionApprovedOn { get; set; }
        public string SubscriptionApprovedByUserId { get; set; }

        public Invoice Invoice { get; set; }
        public ICollection<Distributions> Distributions { get; set; }
    }
    public class Invoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string OrganizationCode { get; set; }
        public long SubscriptionsId { get; set; }
        public Subscriptions Subscriptions { get; set; }
        [Required]
        public string InvoiceNumber { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public DateTimeOffset DateIssued { get; set; } = DateTimeOffset.UtcNow;

        public bool IsApproved { get; set; } = false;
        [Required]
        public string ReferenceCode { get; set; } // code farmer presents for collection

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending; // "Pending", "Approved", "Collected"
        public DateTimeOffset? CollectedOn { get; set; }
        public string CollectedByUserId { get; set; } //who confirmed collection (agent/cooperative head)
        public ICollection<InvoiceItems> InvoiceItems { get; set; }

    }
    public class InvoiceItems
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public long ItemQuantity { get; set; }
        public long InvoiceId { get; set; }
        public Invoice Invoice { get; set; }

    }

    public class Distributions
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        [Required]
        public string OrganizationCode { get; set; }
        [Required]
        public string BusinessCode { get; set; }

        [Required]
        public long SubscriptionId { get; set; }
        public Subscriptions Subscriptions { get; set; }

        public DistributionMode Mode { get; set; } = DistributionMode.Input; // Input or Cash
        public string DistributionReceiverType { get; set; } // "Farmer" or "Cooperative"
        public long DistributionReceiverId { get; set; } // ID of beneficiary

        // if Mode == Cash, this is the amount transferred
        public decimal? Amount { get; set; }
        // if Mode == Input, InputDetails can be a JSON or textual description of items handed
        public string InputDetails { get; set; }

        // Unique reference for the distribution event (can be same as invoice ref or separate)
        [Required] 
        public string ReferenceNumber { get; set; }
        public DistributionStatus Status { get; set; } = DistributionStatus.Pending;

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? DistributedOn { get; set; }
        public string? DistributedByUserId { get; set; } // agent/admin who did the handover

        public ICollection<SubscriptionRepayment> SubscriptionRepayments { get; set; } = new List<SubscriptionRepayment>();
    }
    public class Receipt
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long SubscriptionRepaymentsId { get; set; }
        public SubscriptionRepayment SubscriptionRepayments { get; set; }
        [Required]
        public string ReceiptNumber { get; set; }
        public decimal AmountAcknowledged { get; set; }
        public string Mode { get; set; } // "Cash", "Produce", "Both"
        public DateTimeOffset IssuedOn { get; set; }= DateTimeOffset.UtcNow;

        public ReceiptStatus Status { get; set; } = ReceiptStatus.Partial;// "Partial", "Full"
    }

     public enum InvoiceStatus { Pending, Approved, Collected, Cancelled }
    public enum DistributionStatus { Pending, Distributed, Confirmed, Cancelled }
    public enum DistributionMode { Input, Cash }
    public enum RepaymentMode { Cash, Produce, Hybrid, None }
    public enum RepaymentStatus { Partial, Complete }
    public enum ReceiptStatus { Partial, Full }



    public class OrganizationBundlePlans
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        [Required]
        public string OrganizationCode { get; set; }
        [Required]
        public string BusinessCode { get; set; }

        [Required] 
        public string Name { get; set; }
        public string Description { get; set; }
        public string CropType { get; set; } // e.g. "Maize (Dry Season)"
        public decimal Price { get; set; } // total price of the bundle

        // Eligibility
        public decimal MinHectare { get; set; } = 0;
        public decimal MaxHectare { get; set; } = 9999;

        public decimal CommitmentFee { get; set; } = 0; // default
        public long OrganizationConfigurationId { get; set; } // optional link to a configuration
        public OrganizationConfiguration OrganizationConfiguration { get; set; }

        public ICollection<OrganizationBundleItem> OrganizationBundleItems { get; set; } = new List<OrganizationBundleItem>();
    
        
    }
    public class OrganizationBundleItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        [Required]
        public long OrganizationBundlePlansId { get; set; }
        public OrganizationBundlePlans OrganizationBundlePlans { get; set; }

        [Required] 
        public string Name { get; set; } // "Seed Bag", "Sprayer"
        public string Unit { get; set; } // "kg","litre","piece"
        public decimal Quantity { get; set; } // could be fractional if needed
        public decimal? UnitPrice { get; set; } // optional
    }
}
