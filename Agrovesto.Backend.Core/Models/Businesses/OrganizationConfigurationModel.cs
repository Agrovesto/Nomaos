using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class OrganizationConfigurationModel
    {


        /// <summary>
        /// Unique identifier of the organization
        /// </summary>
        public string? OrganizationCode { get; set; }

        /// <summary>
        /// Organization name
        /// </summary>
        public string? OrganizationName { get; set; }
        public ICollection<WalletProviderConfigurationModel>? WalletProviderConfiguration { get; set; }
        /// <summary>
        /// Input financing module configuration
        /// Specifies how farmers can subscribe, pay for inputs, and repayment structure
        /// </summary>
       public ICollection<InputFinancingConfigurationModel>? InputFinancingConfig { get; set; } 
    }
    public class WalletProviderConfigurationModel
    {
        public long Id { get; set; }
        /// <summary>
        /// Default wallet provider for the organization (e.g., BankAWallet, BankBWallet)
        /// </summary>
        public string DefaultWalletProvider { get; set; }

        /// <summary>
        /// Currency used by organization for wallet and financial transactions
        /// </summary>
        public string Currency { get; set; } = "NGN";

        /// <summary>
        /// Enable multiple wallet accounts per user if true
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Custom provider-specific parameters for wallets
        /// </summary>
     //   public Dictionary<string, string> WalletProviderSettings { get; set; } = new();


    }
    /// <summary>
    /// Configuration for input financing module
    /// </summary>
    public class InputFinancingConfigurationModel
    {
        /// <summary>
        /// Whether input financing is enabled for the organization
        /// </summary>
        public long Id { get; set; }

        public string ConfigurationName { get; set; }
        public string? ConfigurationCode { get; set; }
        public SubscriptionConfigModel SubscriptionConfig { get; set; }
        public DisbursementConfigModel DisbursementConfig { get; set; }
        public RepaymentConfigModel RepaymentConfig { get; set; }
        public bool BundleEnabled { get; set; } = true;
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
        public bool IsEnabled { get; set; } = true;

    }
   

    /// <summary>
    /// Represents a bundle of agricultural inputs for financing
    /// </summary>
    public class SubscriptionConfigModel
    {
        public int Id { get; set; }
        public bool RequiresEquity { get; set; } = true;
        public string EquityType { get; set; } // "Cash", "Sweat", "Land", "Mixed", "None"
        public decimal? EquityPercentage { get; set; }
        public bool CooperativeContributes { get; set; }
        public bool OrganizationContributes { get; set; }
        public bool FinancialPartnerContributes { get; set; }
        public string? ContributionSplitJson { get; set; }
        public bool IsEquityInPercentage { get; set; }
        public bool IsEquityStaticFee { get; set; }
        public bool AllowFarmerSelfSubscription { get; set; }
        public bool AllowCooperativeSubscription { get; set; }
        public decimal EquityStaticFee { get; set; }

        public Dictionary<string, decimal>? ContributionSplits { get; set; }
    }
    public class DisbursementConfigModel
    {
      
        public int Id { get; set; }

        public string DisbursementMode { get; set; } = "Input";// "Input", "Cash", "Both
        public string DisbursementTarget { get; set; } = "Farmer"; // "Farmer", "Cooperative"
        public bool RequiresApprovalForDisbursement { get; set; } = true;
        public bool UsesInvoiceReference { get; set; } = true;
    }
    public class RepaymentConfigModel
    {
        
        public int Id { get; set; }
        public string RepaymentMode { get; set; } = "Cash"; // "Cash", "Produce", "Both", "None"
        public decimal? ExpectedRepaymentPercentage { get; set; } //e.g 100% 50%
        public long RepaymentDurationInMonths { get; set; }
        public bool AllowInstallments { get; set; } = true;
        public bool IsRepaymentInPercentage { get; set; }
        public bool IsRepaymentStaticFee { get; set; }
        public decimal RepaymentStaticFee { get; set; }

    }


}
