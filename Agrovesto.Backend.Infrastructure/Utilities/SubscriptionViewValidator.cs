using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class SubscriptionViewValidator
    {
        public static bool Matches(SubscriptionView view, InputPackageSubscription subscription)
        {
            var status = subscription.Status;
            var repaymentRequired = subscription.InputPackage?.RepaymentRequired ?? false;

            return view switch
            {
                SubscriptionView.PendingApproval =>
                    status == SubscriptionStatus.PendingApproval,

                SubscriptionView.PendingCommitment =>
                    status == SubscriptionStatus.PendingCommitment,

                SubscriptionView.Approved =>
                    status == SubscriptionStatus.Approved,

                SubscriptionView.CommitmentPaid =>
                    status == SubscriptionStatus.CommitmentPaid,

                SubscriptionView.InputReleaseRequest =>
                    status == SubscriptionStatus.PendingReleaseApproval,

                SubscriptionView.PendingDisbursement =>
                    status == SubscriptionStatus.PendingDisbursement,

                SubscriptionView.Active or SubscriptionView.InputReleased =>
                    status == SubscriptionStatus.Disbursed ||
                    status == SubscriptionStatus.PartiallyDisbursed ||
                    status == SubscriptionStatus.Active,

                SubscriptionView.Completed =>
                    status == SubscriptionStatus.Completed ||
                    (!repaymentRequired && status == SubscriptionStatus.Disbursed) ||
                    (!repaymentRequired && status == SubscriptionStatus.Active),

                SubscriptionView.Defaulted =>
                    status == SubscriptionStatus.Defaulted,

                SubscriptionView.Rejected =>
                    status == SubscriptionStatus.Rejected,

                _ => true 
            };
        }
    }
}
