using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class CreditScoreWeightsModel
    {
        public long Id { get; set; }
        public string BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public long RepaymentWeight { get; set; } = 40;
        public long SubcriptionWeight { get; set; } = 15;
        public long TimelinessWeight { get; set; } = 45;
    }

    public class FarmerCreditScoreModel
    {
        public long FarmerId { get; set; }
        public string? NIN { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CooperativeName { get; set; }
        public string BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public decimal Score { get; set; }
        public string RiskLevel { get; set; } = "High";

        public long Id { get; set; }
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;


    }
}
