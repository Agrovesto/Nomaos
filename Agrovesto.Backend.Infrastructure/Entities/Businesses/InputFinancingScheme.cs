using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class InputFinancingScheme
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string InputFinancingConfigurationCode { get; set; }
        public string OrganizationCode { get; set; }
        public string BusinessCode { get; set; }
        public string SchemeName { get; set; }
        public string SchemeCode { get; set; }
        public string CreatedBy { get; set; }
        public string Description { get; set; }
        public string Commodity { get; set; }
        public string BeneficiaryType { get; set; } // Farmer, Cooperative, Both
        public decimal TotalArea { get; set; }
        public decimal TotalValue { get; set; }
        public string FundingSource { get; set; }
        public string Season { get; set; }
        public long WarehouseId { get; set; }

        public long InputFinancingConfigurationId { get; set; }
        public bool IsActive { get; set; }

        public decimal UnitValuePerHectare { get; set; }
        public DateTimeOffset RepaymentStartDate { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset RepaymentEndDate { get; set; }

        public DateTimeOffset SubscriptionOpenDate { get; set; }
        public DateTimeOffset SubscriptionCloseDate { get; set; }

       // public ICollection<SchemeTarget> Targets { get; set; }
    }

    public class SchemeTarget
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long InputFinancingSchemeId { get; set; }
        public string TargetType { get; set; } // Cooperative, FarmerGroup, etc.
        public string TargetId { get; set; }

        public InputFinancingScheme Scheme { get; set; }
    }
}
