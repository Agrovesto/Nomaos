using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.BGService
{
   
    public class RepaymentOverdueProcessor
    {
        private readonly DataContext _context;

        public RepaymentOverdueProcessor(DataContext context)
        {
            _context = context;
        }

        public async Task ProcessOverdueRepaymentsAsync(CancellationToken token)
        {
            var now = DateTimeOffset.UtcNow;

            var overdueSchedules = await _context.RepaymentSchedules
                .Include(x => x.InputPackageSubscription)
                .ThenInclude(x => x.InputPackage)
                .Where(x => !x.IsPaid && x.DueDate < now)
                .ToListAsync(token);

            var affected = new HashSet<long>();

            foreach (var schedule in overdueSchedules)
            {
                var subscription = schedule.InputPackageSubscription;

                // 1. GRACE PERIOD CHECK (KEEP THIS)
                if (subscription.RepaymentGracePeriod >= now)
                    continue;

                // 2. MARK OVERDUE
                schedule.IsOverdue = true;

                // 3. APPLY PENALTY (NEW ADDITION)
                var policy = await _context.PenaltyPolicies
                    .FirstOrDefaultAsync(x =>
                        x.InputPackageId == subscription.InputPackageId &&
                        x.IsActive, token);

                if (policy != null)
                {
                    var penaltyEngine = new PenaltyEngine();

                    var penalty = penaltyEngine.CalculatePenalty(
                        policy,
                        schedule,
                        now);

                    schedule.PenaltyAccrued = penalty;

                    _context.RepaymentPenalties.Add(new RepaymentPenalty
                    {
                        SubscriptionId = subscription.Id,
                        RepaymentScheduleId = schedule.Id,
                        FlatFeeApplied = policy.FlatFee,
                        InterestApplied = penalty - policy.FlatFee,
                        TotalPenalty = penalty,
                        CalculatedOn = now
                    });
                }

                affected.Add(subscription.Id);
            }

            await _context.SaveChangesAsync(token);

            // 4. ESCALATION (KEEP AS IS)
            foreach (var subId in affected)
            {
                await EscalateSubscriptionRisk(subId, token);
            }
        }

        private async Task EscalateSubscriptionRisk(long subscriptionId, CancellationToken token)
        {
            var subscription = await _context.InputPackageSubscriptions
                .FirstOrDefaultAsync(x => x.Id == subscriptionId, token);

            if (subscription == null) return;

            var overdueCount = await _context.RepaymentSchedules
                .CountAsync(x =>
                    x.InputPackageSubscriptionId == subscriptionId &&
                    x.IsOverdue, token);

            subscription.OverdueCount = overdueCount;
            subscription.LastOverdueDetectedOn = DateTimeOffset.UtcNow;

            subscription.RiskCategory = overdueCount switch
            {
                0 => RiskCategory.Low,
                1 => RiskCategory.Medium,
                _ => RiskCategory.High
            };

            if (overdueCount >= 3)
                subscription.Status = SubscriptionStatus.Defaulted;

            _context.Update(subscription);
        }
    }
}
