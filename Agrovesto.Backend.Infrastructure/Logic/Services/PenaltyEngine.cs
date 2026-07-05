using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public class PenaltyEngine
    {
        public decimal CalculatePenalty(
            PenaltyPolicy policy,
            RepaymentSchedule schedule,
            DateTimeOffset now)
        {
            // 1. GRACE CHECK
            var graceEnd = schedule.DueDate.AddDays(policy.GracePeriodDays);

            if (now <= graceEnd)
                return 0;

            // 2. DAYS PAST DUE
            var daysLate = (now - schedule.DueDate).Days;

            if (daysLate <= policy.GracePeriodDays)
                return 0;

            var chargeableDays = daysLate - policy.GracePeriodDays;

            // 3. FLAT FEE
            var flat = policy.FlatFee;

            // 4. DAILY INTEREST
            var interest = schedule.Amount *
                           policy.DailyInterestRate *
                           chargeableDays;

            var total = flat + interest;

            // 5. CAP
            if (total > policy.MaxPenaltyCap)
                total = policy.MaxPenaltyCap;

            return total;
        }
    }
}
