using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class BundleSubscriptionInterestResponseModel
    {
        public string AccountNUmber { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public string Reference { get; set; }
        public decimal Amount { get; set; }
    }
    public class BundleSubscriptionInterestModel
    {
        public long Id { get; set; }
        public string FarmerUserId { get; set; }
        public string? FarmerName { get; set; }
        public long InterestedBundleId { get; set; }
        public string? InterestedBundleName { get; set; }
        public long QuantityOfInterest { get; set; }
        public decimal ExpectedEquityPricePerBundle { get; set; }
        public decimal ExpectedTotalEquityPrice { get; set; }
        public decimal TotalEquityPaid { get; set; }
        public decimal EquityPaymentBalanceDeficit { get; set; }
        public long TotalQuantityPaidFor { get; set; }
        public EquityPaymentStatus PaymentStatus { get; set; }
        public DateTimeOffset? PaymentDate { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public decimal EquityPlusCommission { get; set; }
        public bool IsCommissionPaidOut { get; set; }//has Agrovesto commission been paid

        public bool ShouldAddCommission { get; set; }
        public decimal CommissionPrice { get; set; }
        public double CommissionPercentage { get; set; }

        public string? BelongToBusinessCode { get; set; }
        public string? OfficerInChargeUserId { get; set; }
        public string? Reference { get; set; }
        public string? Location { get; set; }
        public long CooperativeId { get; set; }
        public string? AccountBalance { get; set; }
    }
}
