using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
   
    public class RepaymentModel
    {
         public long Id { get; set; }
        public long FarmerId { get; set; }
        public long InterestedBundleId { get; set; }
        public string? OrganizationCode { get; set; }
        public string BelongToBusinessCode { get; set; }
        public DateTimeOffset? CompletedOn { get; set; }
        public DateTimeOffset PaymentDueDate { get; set; }
        public decimal AmountToRepay { get; set; } //same as EquityPaymentBalanceDeficit
        public decimal AmountPaid { get; set; }
        public bool IsComplete { get; set; }
        public List<RepaymentBreakdownModel> RepaymentBreakdown { get; set; }

    }
    public class RepaymentBreakdownModel
    {
          public long Id { get; set; }
          public long RepaymentId { get; set; }
           public RepaymentMode RepaymentMode { get; set; } //Cash/Crop/both
        public decimal AmountOrEquivalent { get; set; }
        public DateTimeOffset PaidOn { get; set; }
         }
    public enum RepaymentMode
    {
       Cash,
       Crop,


    }
}
