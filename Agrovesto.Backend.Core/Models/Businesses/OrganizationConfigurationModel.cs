using System;
using System.Collections.Generic;
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
        public WalletProviderConfigurationModel WalletProviderConfiguration { get; set; }
        /// <summary>
        /// Input financing module configuration
        /// Specifies how farmers can subscribe, pay for inputs, and repayment structure
        /// </summary>
     //  public InputFinancingConfiguration InputFinancingConfig { get; set; } = new();
    }
    public class WalletProviderConfigurationModel
    {
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
        public bool EnableMultiWallet { get; set; } = false;

        /// <summary>
        /// Custom provider-specific parameters for wallets
        /// </summary>
     //   public Dictionary<string, string> WalletProviderSettings { get; set; } = new();


    }
    /// <summary>
    /// Configuration for input financing module
    /// </summary>
    public class InputFinancingConfiguration
    {
        /// <summary>
        /// Whether input financing is enabled for the organization
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Default repayment method (Percentage of harvest, Cash, or Combination)
        /// </summary>
        public string DefaultRepaymentMethod { get; set; } = "PercentageOfHarvest";

        /// <summary>
        /// Subscription options: list of bundles, quantities, and pricing
        /// </summary>
        public List<InputBundleOption> BundleOptions { get; set; } = new();

        /// <summary>
        /// Maximum financing limit per farmer
        /// </summary>
        public decimal MaxFinancingAmount { get; set; } = 0;

        /// <summary>
        /// Whether farmers need to be part of a cooperative
        /// </summary>
        public bool RequiresCooperativeMembership { get; set; } = true;
    }

    /// <summary>
    /// Represents a bundle of agricultural inputs for financing
    /// </summary>
    public class InputBundleOption
    {
        public string BundleName { get; set; }
        public string InputType { get; set; } // e.g., Seed, Fertilizer, Agrochemicals
        public int Units { get; set; }
        public decimal Price { get; set; }
    }

}
