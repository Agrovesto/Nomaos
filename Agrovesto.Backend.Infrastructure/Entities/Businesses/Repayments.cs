using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class Repayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string FarmerUserId { get; set; }
        public long InterestedBundleId { get; set; }
        public string? OrganizationCode { get; set; }
        public string BelongToBusinessCode { get; set; }
        public DateTimeOffset? CompletedOn { get; set; }
        public DateTimeOffset PaymentDueDate { get; set; }
        public decimal AmountToRepay { get; set; } //same as EquityPaymentBalanceDeficit
        public decimal AmountPaid { get; set; }
        public bool IsComplete { get; set; }
        public List<RepaymentBreakdown> RepaymentBreakdown { get; set; }

    }
    public class RepaymentBreakdown
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
         public long RepaymentId { get; set; }
        public string RepaymentMode { get; set; } //Cash/Crop/both
        public decimal AmountOrEquivalent {  get; set; }
        public DateTimeOffset PaidOn { get; set; }
        public virtual Repayment Repayment { get; set; }
    }
}
