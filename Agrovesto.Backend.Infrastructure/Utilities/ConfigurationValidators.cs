using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    using Agrovesto.Backend.Core.Models.Businesses;
    using Newtonsoft.Json;

    public static class ConfigurationValidator
    {
        public static (bool IsValid, string Message) ValidateConfiguration(InputFinancingConfigurationModel config)
        {
            if (config == null)
                return (false, "Configuration cannot be null.");

            // 1️⃣ Validate Subscription / Contribution Split
            var (isSplitValid, splitMsg) = ValidateContributionSplit(config.SubscriptionConfig);
            if (!isSplitValid)
                return (false, splitMsg);

            // 2️⃣ Validate Repayment Rules
            var (isRepaymentValid, repaymentMsg) = ValidateRepaymentRules(config.RepaymentConfig);
            if (!isRepaymentValid)
                return (false, repaymentMsg);

            // 3️⃣ Validate Disbursement Logic Compatibility
            var (isDisbursementValid, disbMsg) = ValidateDisbursementRules(config.DisbursementConfig, config.SubscriptionConfig);
            if (!isDisbursementValid)
                return (false, disbMsg);

            return (true, "Configuration is valid.");
        }

        // ========================
        // ✅ Contribution Split
        // ========================
        private static (bool, string) ValidateContributionSplit(SubscriptionConfigModel config)
        {
            if (config == null)
                return (false, "Subscription configuration cannot be null.");

            if (!config.RequiresEquity)
                return (true, "Equity not required — contribution split not needed.");

            bool hasContributors = config.CooperativeContributes || config.OrganizationContributes || config.FinancialPartnerContributes;
            if (!hasContributors)
                return (true, "No active contributors — contribution split not needed.");

            if (string.IsNullOrWhiteSpace(config.ContributionSplitJson))
                return (false, "Contribution split JSON must be provided when contributors are active.");

            Dictionary<string, decimal>? split;
            try
            {
                split = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(config.ContributionSplitJson);
            }
            catch
            {
                return (false, "Invalid JSON format for ContributionSplitJson.");
            }

            if (split == null || !split.Any())
                return (false, "Contribution split configuration is missing or empty.");

            decimal total = split.Values.Sum();
            if (total != 100)
                return (false, $"Contribution split total must equal 100%. Current total: {total}%.");

            var activeContributors = new List<string>();
            if (config.CooperativeContributes) activeContributors.Add("Cooperative");
            if (config.OrganizationContributes) activeContributors.Add("Organization");
            if (config.FinancialPartnerContributes) activeContributors.Add("FinancialPartner");

            var invalidContributors = split.Keys.Except(activeContributors, StringComparer.OrdinalIgnoreCase).ToList();
            if (invalidContributors.Any())
                return (false, $"Invalid contributors found in split: {string.Join(", ", invalidContributors)}.");

            return (true, "Contribution split configuration is valid.");
        }

        // ========================
        // 💰 Repayment Validation
        // ========================
        private static (bool, string) ValidateRepaymentRules(RepaymentConfigModel config)
        {
            if (config == null)
                return (false, "Repayment configuration cannot be null.");

            if (config.ExpectedRepaymentPercentage <= 0)
                return (false, "Expected repayment percentage must be greater than zero.");

            if (config.RepaymentDurationInMonths <= 0)
                return (false, "Repayment duration must be at least one month.");

            // Prevent conflicts in repayment settings
            if (config.IsRepaymentInPercentage && config.IsRepaymentStaticFee)
                return (false, "Repayment cannot be both percentage-based and static fee.");

            if (!config.IsRepaymentInPercentage && !config.IsRepaymentStaticFee && !config.AllowInstallments)
                return (false, "At least one repayment option (percentage, static fee, or installments) must be enabled.");

            if (config.IsRepaymentStaticFee && config.RepaymentStaticFee <= 0)
                return (false, "Static fee amount must be greater than zero when enabled.");

            // Logical consistency for mode
            var validModes = new[] { "Cash", "BankTransfer", "Wallet" };
            if (!validModes.Contains(config.RepaymentMode))
                return (false, $"Invalid repayment mode: {config.RepaymentMode}. Allowed: {string.Join(", ", validModes)}.");

            return (true, "Repayment configuration is valid.");
        }

        // ========================
        // 💳 Disbursement Logic
        // ========================
        private static (bool, string) ValidateDisbursementRules(DisbursementConfigModel disb, SubscriptionConfigModel sub)
        {
            if (disb == null)
                return (false, "Disbursement configuration cannot be null.");

            var validModes = new[] { "Cash", "BankTransfer", "Wallet" };
            if (!validModes.Contains(disb.DisbursementMode))
                return (false, $"Invalid disbursement mode: {disb.DisbursementMode}. Allowed: {string.Join(", ", validModes)}.");

            // Compatibility: Only Cooperative or Farmer allowed, not both
            if (sub.AllowFarmerSelfSubscription && sub.AllowCooperativeSubscription)
                return (false, "Configuration cannot allow both farmer and cooperative subscription types.");

            // Compatibility check between disbursement target and subscription type
            if (disb.DisbursementTarget == "Farmer" && !sub.AllowFarmerSelfSubscription)
                return (false, "Disbursement target is Farmer, but configuration doesn't allow farmer subscription.");

            if (disb.DisbursementTarget == "Cooperative" && !sub.AllowCooperativeSubscription)
                return (false, "Disbursement target is Cooperative, but configuration doesn't allow cooperative subscription.");

            return (true, "Disbursement configuration is valid.");
        }
    }

}
