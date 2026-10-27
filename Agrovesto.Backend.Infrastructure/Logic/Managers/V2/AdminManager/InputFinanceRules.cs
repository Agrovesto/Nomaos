using Agrovesto.Backend.Core.Models.Businesses.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
    public static class InputFinanceRules
    {
        public static readonly SubscriptionStatus[] FinancedStatuses =
        {
        SubscriptionStatus.Disbursed, SubscriptionStatus.PartiallyDisbursed,
        SubscriptionStatus.Active, SubscriptionStatus.PendingRepayment,
        SubscriptionStatus.Completed, SubscriptionStatus.Defaulted
    };

        public static readonly SubscriptionStatus[] ActiveStatuses =
        {
        SubscriptionStatus.Active, SubscriptionStatus.Completed, SubscriptionStatus.Defaulted,
        SubscriptionStatus.PartiallyDisbursed, SubscriptionStatus.PendingRepayment
    };

        public static string ToBadge(SubscriptionStatus s) => s switch
        {
            SubscriptionStatus.Completed => "Completed",
            SubscriptionStatus.Defaulted => "Defaulted",
            _ => "Active"
        };

        public static string FormatNaira(decimal amount)
        {
            static string Trim(decimal v)
            {
                var truncated = Math.Truncate(v * 100m) / 100m;     // 2 decimals, no rounding
                return truncated.ToString("0.##", CultureInfo.InvariantCulture);
            }

            if (amount >= 1_000_000_000m) return $"₦{Trim(amount / 1_000_000_000m)}B";
            if (amount >= 1_000_000m) return $"₦{Trim(amount / 1_000_000m)}M";
            if (amount >= 1_000m) return $"₦{Trim(amount / 1_000m)}K";
            return $"₦{Trim(amount)}";
        }
    }
}
