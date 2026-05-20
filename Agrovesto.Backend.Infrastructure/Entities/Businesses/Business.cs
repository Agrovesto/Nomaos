using Agrovesto.Backend.Infrastructure.Entities.Common;
using Amazon.Rekognition.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class MigrationTracker
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long BusinessId { get; set; }
        public string Name { get; set; }
        public bool Completed { get; set; }
        public DateTimeOffset RunOn { get; set; }
    }


    public class Business
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BusinessId { get; set; }
        [Required]
        public string BusinessCode { get; set; }
        [Required]
        public string BusinessCodeSignage { get; set; }
        [Required]
        public string BusinessCodeSignageSalt { get; set; }
        public string UserId { get; set; }
        public string BusinessName { get; set; }
        public string ContactEmail { get; set; }
        public string? SupportEmail { get; set; }
        public string? DisputeEmail { get; set; }
        public string BusinessEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string? Website { get; set; }
        public string? BVN { get; set; }
        public string? NIN { get; set; }
        public string BusinessStatus { get; set; }
        public string Category { get; set; }
        public string KycCategory { get; set; }
        public string BusinessLogo { get; set; }
        public string? PropagateUrl { get; set; }
        /*
         This map to business categories in the UI
         */
        public ICollection<BusinessWorker> BusinessWorkers { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTime.Now;
        public ICollection<SubAccount> SubAccounts { get; set; } = new HashSet<SubAccount>(); //subaccounts are the business users
                                                                                              //  public List<Product> Products { get; set; } // product maps agrovesto offerings to the business configuration of selected products
        public ICollection<Common.User> users { get; set; } = new List<Common.User>();
        public bool IsActive { get; set; } = true;
        public bool IsSuspended { get; set; } = false;
        public string? SuspensionReason { get; set; }

        public ICollection<BusinessLicense> Licenses { get; set; }
    = new HashSet<BusinessLicense>();
        public string? ThemeColor { get; set; }

        public string? SecondaryColor { get; set; }

        public string? LogoUrl { get; set; }

        public string? FaviconUrl { get; set; }

        public string? CustomDomain { get; set; }

        public string? EmailSenderName { get; set; }
    }
    public class BusinessLicenseModule
    {
        [Key]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long BusinessLicenseId { get; set; }
        public BusinessLicense BusinessLicense { get; set; }

        public long SystemModuleId { get; set; }
        public SystemModule SystemModule { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTimeOffset EnabledOn { get; set; }
            = DateTimeOffset.UtcNow;
    }
    public class SystemModule
    {
        [Key]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public int DisplayOrder { get; set; }
        public bool RequiresSubscription { get; set; }
    }   
    public class BusinessLicense
    {
        [Key]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long BusinessId { get; set; }
        public Business Business { get; set; }

        // Optional reference/license key
        public string LicenseKey { get; set; }

        // Example:
        // Trial
        // Annual
        // Enterprise
        // WhiteLabel
        public string LicenseType { get; set; }

        // Start/end dates
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        // Duration snapshot
        public int DurationInMonths { get; set; }

        // License status
        public bool IsActive { get; set; } = true;

        // Grace period support
        public int GracePeriodDays { get; set; } = 0;

        // Audit
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; }

        // Renewal support
        public bool AutoRenew { get; set; } = false;

        // Optional feature control
        public ICollection<BusinessLicenseModule> Modules
        { get; set; }
     = new HashSet<BusinessLicenseModule>();

        // Payment/reference tracking
        public string? PaymentReference { get; set; }

        // Reason for deactivation
        public string? DeactivatedReason { get; set; }
    }

    public class BusinessBillingInvoice
    {
        public long Id { get; set; }

        public long BusinessId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }

        public string Status { get; set; }

        public DateTimeOffset DueDate { get; set; }

        public ICollection<BusinessPayment> BusinessPayments { get; set; }
    }

    public class BusinessPayment
    {
        public long Id { get; set; }

        public long BusinessBillingInvoiceId { get; set; }

        public decimal AmountPaid { get; set; }

        public string PaymentReference { get; set; }

        public string PaymentMethod { get; set; }

        public DateTimeOffset PaidOn { get; set; }
    }
}
