using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class EnterpriseBusinessCosting
    {
        public long Id { get; set; }
        public string BusinessCode { get; set; }
        public string BusinessName { get; set; }
        public decimal DevelopmentFee { get; set; }
        public decimal InfrastructureFeeChargedToAgrovesto { get; set; }
        public decimal InfrastructureFeeChargedToAgrovestoInDollar { get; set; }
        public decimal InfrastructureFeeReceivedFromBusiness { get; set; }
        public decimal InfrastructureFeeReceivedFromBusinessInDollar { get; set; }
        public decimal MaintenanceFee { get; set; }
        public decimal AgrovestoMargin { get; set; }
        public decimal Total { get; set; }

        public string PaymentStatus { get; set; }
        public string? OrganizationCode { get; set; }
    }
    public class EnterpriseBusinessCostingPaymentDetail
    {
        public long EnterpriseBusinessCostingId { get; set; }
        public long Id { get; set; }
        public DateTimeOffset PaidOn { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public string? OrganizationCode { get; set; }
    }
}
