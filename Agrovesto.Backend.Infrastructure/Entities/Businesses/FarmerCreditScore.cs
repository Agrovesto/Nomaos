using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class FarmerCreditScore
    {
        public long FarmerId { get; set; }
        public string BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public decimal Score { get; set; } = 0; //0-100 scale
        public long TotalSubscriptions { get; set; } = 0;
        public long CompletedRepayments { get; set; } = 0;
        public long DefaultedRepayments { get; set; } = 0;

        public string RiskLevel { get; set; } = "High";
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;
       // public string NIN { get;set; }

    }

    public class CooperativeCreditScore
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        [Required] 
        public long CooperativeId { get; set; }
        public decimal Score { get; set; } = 0;
        public long TotalSubscriptions { get; set; } = 0;
        public long CompletedRepayments { get; set; } = 0;
        public long DefaultedRepayments { get; set; } = 0;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
    public class CreditScoreWeights
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public long RepaymentWeight { get; set; } = 40;
        public long SubcriptionWeight { get; set; } = 15;
        public long TimelinessWeight { get; set; } =45;
    }



}
