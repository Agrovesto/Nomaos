using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.V2.Events
{
    public class InputPackageCreatedEvent
    {
        public long PackageId { get; set; }
        public long? OrganizationId { get; set; }
        public string CreatedByUserId { get; set; }
    }
    public class InputPackageConfigurationUpdatedEvent
    {
        public long PackageId { get; set; }
        public long? OrganizationId { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }
    public class InputPackageSubscriberAddedEvent
    {
        public long SubscriptionId { get; set; }
        public long PackageId { get; set; }
        public string SubscriberUserId { get; set; }

        public string SubscriberType { get; set; }

        public string Status { get; set; }

        public bool CommitmentRequired { get; set; }

        public decimal CommitmentValue { get; set; }

        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }
    public class InputPackageSubscriptionApprovedEvent
    {
        public long SubscriptionId { get; set; }

        public long PackageId { get; set; }

        public string SubscriberUserId { get; set; }

        public bool RequiresCommitment { get; set; }

        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }

    public class InputPackageSubscriptionRejectedEvent
    {
        public long SubscriptionId { get; set; }

        public long PackageId { get; set; }

        public string SubscriberUserId { get; set; }

        public string RejectionReason { get; set; }

        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }
    public class CommitmentPaymentRecordedEvent
    {
        public long SubscriptionId { get; set; }

        public long PackageId { get; set; }

        public decimal Amount { get; set; }

        public string PaidByUserId { get; set; }
        

        public string SubscriberUserId { get; set; }

        public string AgentUserId { get; set; }

        public long? OrganizationId { get; set; }

        public decimal AmountPaid { get; set; }

        public decimal OutstandingBalance { get; set; }

        public string PackageName { get; set; }

        public bool FullyPaid { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }
 
    public class CommitmentFullyPaidEvent
    {
        public long SubscriptionId { get; set; }

        public long PackageId { get; set; }

        public decimal TotalPaid { get; set; }

        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }
    public class SubscriptionDisbursedEvent
    {
        public long SubscriptionId { get; set; }

        public long PackageId { get; set; }

        public decimal Amount { get; set; }

        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }
    public class RepaymentRecordedEvent
    {
        public long SubscriptionId { get; set; }

        public decimal RemainingAmount { get; set; }

        public decimal Amount { get; set; }

        public bool IsCompleted { get; set; }
    }
    public class OverdueDetectedEvent
    {

    }
    public class LoanDefaultedEvent
    {

    }
}
