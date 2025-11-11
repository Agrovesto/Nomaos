using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class OrganizationConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        /// <summary>
        /// Unique identifier of the organization
        /// </summary>
        public string OrganizationCode { get; set; }

        /// <summary>
        /// Organization name
        /// </summary>
        public string OrganizationName { get; set; }
        public ICollection<WalletProviderConfiguration>? WalletProviderConfigurations { get; set; }
        public ICollection<InputFinancingConfiguration>? InputFinancingConfigurations { get; set; }



    }
    public class WalletProviderConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Default wallet provider for the organization (e.g., BankAWallet, BankBWallet)
        /// </summary>
        public string? DefaultWalletProvider { get; set; }

        /// <summary>
        /// Currency used by organization for wallet and financial transactions
        /// </summary>
        public string Currency { get; set; } = "NGN";

        /// <summary>
        /// Enable multiple wallet accounts per user if true
        /// </summary>
        public bool IsDefault { get; set; } = false;


        public long OrganizationConfigurationId { get; set; }
        public OrganizationConfiguration OrganizationConfiguration { get; set; }
    }
   public class InputFinancingConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string ConfigurationCode { get; set; }
        public SubscriptionConfig SubscriptionConfigs { get; set; } 
        public DisbursementConfig DisbursementConfigs { get; set; } 
        public RepaymentConfig RepaymentConfigs { get; set; } 

        public bool BundleEnabled { get; set; } = true;
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
        public bool IsEnabled { get; set; } = true;
        public long OrganizationConfigurationId { get; set; }
        public OrganizationConfiguration OrganizationConfiguration { get; set; }
    }
    public class SubscriptionConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }
        public bool RequiresEquity { get; set; } = true;
        public string EquityType { get; set; } // "Cash", "Sweat", "Land", "Mixed", "None"
        public decimal? EquityPercentage { get; set; }
        public bool IsEquityInPercentage { get; set; }
        public bool IsEquityStaticFee { get; set; }
        public decimal EquityStaticFee { get; set; }
        public bool CooperativeContributes { get; set; }
        public bool OrganizationContributes { get; set; }
        public bool FinancialPartnerContributes { get; set; }
        public string? ContributionSplitJson { get; set; }
        public bool AllowFarmerSelfSubscription { get; set; }
        public bool AllowCooperativeSubscription { get; set; }

    }
    public class DisbursementConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }

        public string DisbursementMode { get; set; } = "Input";// "Input", "Cash", "Both
        public string DisbursementTarget { get; set; } = "Farmer"; // "Farmer", "Cooperative"
        public bool RequiresApprovalForDisbursement { get; set; } = true;
        public bool UsesInvoiceReference { get; set; } = true;
    }
    public class RepaymentConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }
        public string RepaymentMode { get; set; } = "Cash"; // "Cash", "Produce", "Both", "None"
        public decimal? ExpectedRepaymentPercentage { get; set; } //e.g 100% 50%
        public long RepaymentDurationInMonths { get; set; }
        public bool AllowInstallments { get; set; } = true;
        public bool IsRepaymentInPercentage { get; set; }
        public bool IsRepaymentStaticFee { get; set; }
        public decimal RepaymentStaticFee { get; set; }

    }


    public class BundlePlan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }
        public string OrganizationCode { get; set; }
        public string Name { get; set; }
        public string CropType { get; set; }
        public decimal Price { get; set; }
        public ICollection<BundleItem> Items { get; set; } = new List<BundleItem>();

        public decimal CommitmentFee { get; set; }
        public decimal MinHectare { get; set; }
        public decimal MaxHectare { get; set; }
    }
    public class BundleItem
    {
        public int Id { get; set; }

        public int BundlePlanId { get; set; }
        public BundlePlan BundlePlan { get; set; }  // Navigation property

        public string Name { get; set; }   // e.g. "Seed Bag", "Sprayer", "Chemical"
        public string Unit { get; set; }   // e.g. "kg", "litre", "piece"
        public int Quantity { get; set; }  // e.g. 2 seed bags, 1 sprayer
        public decimal Price { get; set; } // Optional: price per unit if you want detailed costing
    }

}
