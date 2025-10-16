using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class CreateInputFinancingSchemeDto
    {
        public string SchemeName { get; set; }
        public string Description { get; set; }
        public string Commodity { get; set; }
        public string BeneficiaryType { get; set; }
        public decimal TotalArea { get; set; }
        public decimal TotalValue { get; set; }
        public string FundingSource { get; set; }
        public string Season { get; set; }
        public string WarehouseId { get; set; }

        public long InputFinancingConfigurationId { get; set; }
        public bool IsActive { get; set; }

        public decimal UnitValuePerHectare { get; set; }
        public DateTimeOffset RepaymentStartDate { get; set; }
        public DateTimeOffset RepaymentEndDate { get; set; }

        public DateTimeOffset SubscriptionOpenDate { get; set; }
        public DateTimeOffset SubscriptionCloseDate { get; set; }

     //   public List<SchemeTargetDto> Targets { get; set; }
    }

    public class SchemeTargetDto
    {
        public string TargetType { get; set; }
        public string TargetId { get; set; }
    }

    public class InputFinancingSchemeDto
    {
        public long Id { get; set; }

        public string? OrganizationCode { get; set; }
        public string? BusinessCode { get; set; }
        public string SchemeName { get; set; }
        public string? CreatedBy { get; set; }
        public string SchemeCode { get; set; }
        public string Description { get; set; }
        public string Commodity { get; set; }
        public string BeneficiaryType { get; set; }
        public decimal TotalArea { get; set; }
        public decimal TotalValue { get; set; }
        public string FundingSource { get; set; }
        public string Season { get; set; }
       // public long? WarehouseId { get; set; }

        public long InputFinancingConfigurationId { get; set; }
        public bool IsActive { get; set; }

        public decimal UnitValuePerHectare { get; set; }
        public DateTimeOffset RepaymentStartDate { get; set; }
        public DateTimeOffset RepaymentEndDate { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset SubscriptionOpenDate { get; set; }
        public DateTimeOffset SubscriptionCloseDate { get; set; }
        public string ConfigurationCode { get; set; }
       // public List<SchemeTargetDto> Targets { get; set; }
    }
}
