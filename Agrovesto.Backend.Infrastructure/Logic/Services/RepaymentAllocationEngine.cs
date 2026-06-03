using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public class RepaymentAllocationEngine
    {
        public SubscriptionPaymentAllocation Allocate(
            decimal paymentAmount,
            decimal outstandingPenalty,
            decimal outstandingInterest,
            decimal outstandingPrincipal)
        {
            var allocation = new SubscriptionPaymentAllocation();

            var remaining = paymentAmount;

            // 1. PENALTY FIRST
            allocation.PenaltyPortion = Math.Min(remaining, outstandingPenalty);
            remaining -= allocation.PenaltyPortion;

            // 2. INTEREST SECOND
            allocation.InterestPortion = Math.Min(remaining, outstandingInterest);
            remaining -= allocation.InterestPortion;

            // 3. PRINCIPAL LAST
            allocation.PrincipalPortion = Math.Min(remaining, outstandingPrincipal);

            return allocation;
        }
    }
}
