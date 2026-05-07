using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses.Input
{
    public enum AdditionalCostType
    {
        Logistics = 1,
        Labor = 2,
        Insurance = 3,
        Service = 4,
        Other = 5
    }
    public enum RepaymentScheduleType
    {
        Instant,
        Installment,
        HarvestLinked
    }
    public enum InputPackageStatus
    {
        Draft = 1,
        Published = 2,
        Suspended = 3,
        Archived = 4
    }
    public enum InputPackageRepaymentMode
    {
        Cash = 1,
        Produce = 2,
        Hybrid = 3,
        RevenueShare = 4
    }
    public enum RiskCategory
    {
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum InputSubScriptionType
    {
        Individual = 1,
        MemberShip = 2,
        Both = 3,
    }
    public enum InputPackageStructure
    {
        Physical = 1, // Pre-defined list of inputs with fixed quantities and costs
        Monetary = 2 // Farmer receives a cash amount to purchase inputs themselves
    }
    public enum CommitmentType
    {
        StaticFee = 1,
        PercentageOfTotalValue = 2,
        Custom = 3
    }
    public enum SubscriptionStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Disbursed = 4,
        Active = 5,
        Completed = 6,
        Defaulted = 7
    }
    public enum DisbursementType
    {
        Cash = 1,
        Input = 2,
        Hybrid = 3
    }

    public enum DisbursementStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3
    }
    public enum RepaymentType
    {
        Cash = 1,
        Produce = 2
    }

    public enum PaymentChannel
    {
        Wallet = 1,
        BankTransfer = 2,
        Cash = 3,
        Offtake = 4
    }

    public enum TransactionStatus
    {
        Pending = 1,
        Successful = 2,
        Failed = 3
    }
}
