using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class InputFinancingModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string RequestingFarmerName { get; set; }
        public long RequestingFarmerId { get; set; }
        public string BundlePlanSubscribedTo { get; set; }
        //check if quantity left in bundle is enough to cater for quantity subscribed to,, 
        //i.e available - quantity subscribed to
        public long QuantitySubscribedTo { get; set; }
        public long QuantityReceived { get; set; }
        public long SubscriptionBalance { get; set; }
        public InputFinancingStatus Status { get; set; }
        public decimal EquityPaidForSubscription { get; set; }
        public decimal EquityBalanceForSubscription { get; set; }
        public decimal EquityToPayForSubscription { get; set; }
        public EquityPaymentStatus EquityPaymentStatus { get; set; }
    }
    public enum EquityPaymentStatus
    {
        Initial,
        Pending,
        Partial_Payment,
        Paid,
        Failed

    }
    public enum InputFinancingStatus
    {
        Requested,
        Disbursed,
        PartiallyDisbursed
    }

    //public class BundlePlanSubcription
    //{
    //    [Key]
    //    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    //    public long Id { get; set; }
    //    public string SubscribedToPlan { get; set; }
    //    public long RequestingFarmerId { get; set; }
    //}
}
