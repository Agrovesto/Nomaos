using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses.Input
{
    public enum AdditionalCostType
    {
        [Display(Name = "Logistics")]
        Logistics = 1,

        [Display(Name = "Labor")]
        Labor = 2,

        [Display(Name = "Insurance")]
        Insurance = 3,

        [Display(Name = "Service")]
        Service = 4,

        [Display(Name = "Other")]
        Other = 5
    }
    public enum AdditionalCostTreatmentType
    {
        [Display(Name ="Internal")]
        Internal = 1,
        [Display(Name = "Paid To User")]
        PaidOutToUser = 2,
        [Display(Name = "Paid To Vendor")]
        VendorPaid = 3
    }
    public enum RepaymentScheduleType
    {
        [Display(Name = "Installment")]
        Installment = 1,

        [Display(Name = "Instant")]
        Instant = 2,

        [Display(Name = "Harvest Linked")]
        HarvestLinked = 3
    }

    public enum InputPackageStatus
    {
        [Display(Name = "Draft")]
        Draft = 1,

        [Display(Name = "Published")]
        Published = 2,

        [Display(Name = "Suspended")]
        Suspended = 3,

        [Display(Name = "Archived")]
        Archived = 4
    }

    public enum InputPackageRepaymentMode
    {
        [Display(Name = "Cash")]
        Cash = 1,

        [Display(Name = "Produce")]
        Produce = 2,

        [Display(Name = "Hybrid")]
        Hybrid = 3,

        [Display(Name = "Revenue Share")]
        RevenueShare = 4
    }

    public enum RepaymentFrequency
    {
        [Display(Name = "Not Set")]
        NA = 0,

        [Display(Name = "Weekly")]
        Weekly = 1,

        [Display(Name = "Bi Weekly")]
        BiWeekly = 2,

        [Display(Name = "Monthly")]
        Monthly = 3,

        [Display(Name = "Quarterly")]
        Quarterly = 4,

        [Display(Name = "Annually")]
        Annually = 5
    }

    public enum RiskCategory
    {
        [Display(Name = "Low")]
        Low = 1,

        [Display(Name = "Medium")]
        Medium = 2,

        [Display(Name = "High")]
        High = 3
    }

    public enum InputSubScriptionType
    {
        [Display(Name = "Individual")]
        Individual = 1,

        [Display(Name = "Membership")]
        MemberShip = 2,

        [Display(Name = "Both")]
        Both = 3,
    }

    public enum InputPackageStructure
    {
        [Display(Name = "Physical")]
        Physical = 1,

        [Display(Name = "Monetary")]
        Monetary = 2
    }

    public enum CommitmentType
    {
        [Display(Name = "Static Fee")]
        StaticFee = 1,

        [Display(Name = "Percentage Of Total Value")]
        PercentageOfTotalValue = 2,

        [Display(Name = "Custom")]
        Custom = 3
    }

    public enum SubscriptionStatus
    {
        [Display(Name = "Pending Approval")]
        PendingApproval = 1,

        [Display(Name = "Approved")]
        Approved = 2,

        [Display(Name = "Pending Commitment")]
        PendingCommitment = 3,


        [Display(Name = "Commitment Paid")]
        CommitmentPaid = 4,

        [Display(Name = "Pending Disbursement")]
        PendingDisbursement = 5,

        [Display(Name = "Disbursed")]
        Disbursed = 6,

        [Display(Name = "Active")]
        Active = 7,

        [Display(Name = "Completed Repayment")]
        RepaymentCompleted = 8,

        [Display(Name = "Defaulted")]
        Defaulted = 9,

        [Display(Name = "Rejected")]
        Rejected = 10,

        [Display(Name = "Partially Disbursed")]
        PartiallyDisbursed = 11
    }

    public enum DisbursementType
    {
        [Display(Name = "Cash")]
        Cash = 1,

        [Display(Name = "Input")]
        Input = 2,

        [Display(Name = "Hybrid")]
        Hybrid = 3
    }

    public enum DisbursementStatus
    {
        [Display(Name = "Pending")]
        PendingApproval = 1,

        [Display(Name = "Approved")]
        Approved = 2,

        [Display(Name = "Rejected")]
        Rejected = 3,


        [Display(Name = "Disbursed")]
        Disbursed = 4,

        [Display(Name = "Failed")]
        Failed = 5
    }

    public enum RepaymentType
    {
        [Display(Name = "Cash")]
        Cash = 1,

        [Display(Name = "Produce")]
        Produce = 2
    }

    public enum PaymentChannel
    {
        [Display(Name = "Wallet")]
        Wallet = 1,

        [Display(Name = "Offline Bank Transfer")]
        OffilineBankTransfer = 2,

        [Display(Name = "Cash")]
        Cash = 3,

        [Display(Name = "Payment Gateway")]
        PaymentGateway = 4,

        [Display(Name = "Offtake")]
        Offtake = 5,

        [Display(Name = "POS")]
        POS = 6,


    }

    public enum TransactionStatus
    {
        [Display(Name = "Pending")]
        Pending = 1,

        [Display(Name = "Processing")]
        Processing = 2,

        [Display(Name = "Successful")]
        Successful = 3,

        [Display(Name = "Failed")]
        Failed = 4,

        [Display(Name ="Cancelled")]
        Cancelled = 5,

        [Display(Name = "Awaiting Confirmation")]
        AwaitingConfirmation = 6,

        [Display(Name = "Awaiting Reconciliation")]
        AwaitingReconciliation = 7,
    }
    public enum DocumentType
    {
        [Display(Name = "Commission Letter")]
        CommissionLetter =1,

    }

    public enum ApprovalDecision
    {
        [Display(Name = "Approved")]
        Approved = 1,
        [Display(Name = "Rejected")]
        Rejected = 2
    }

    public enum PaymentType
    {
        [Display(Name = "Commitment")]
        Commitment = 1,
        [Display(Name = "Repayment")]
        Repayment = 2
      
    }

    public enum Stage
    {
        [Display(Name = "SoftReminder")]
        SoftReminder = 1,
        [Display(Name = "HardReminder")]
        HardReminder = 2,
        [Display(Name = "Legal")]
        Legal = 3
    }

    public enum SubscriptionView
    {
        [Display(Name = "All")]
        All=0,

        [Display(Name = "Pending Approval")]
        PendingApproval =1,

        [Display(Name = "Approved")]
        Approved =2,

        [Display(Name = "Pending Commitment")]
        PendingCommitment =3,

        [Display(Name = "Pending Disbursement")]
        PendingDisbursement =4,

        [Display(Name = "Active")]
        Active =5,

        [Display(Name = "Repayment Completed")]
        Completed =6,


        [Display(Name = "Defaulted")]
        Defaulted =7
    }

    public enum ExportFormat
    {
        Excel=1,
        Pdf=2
    }

    public enum InterestType
    {
        Flat =1,
        Dailt=2,
        Weekly=3,
        Monthly=4,
        Compound=5,
    }
}
