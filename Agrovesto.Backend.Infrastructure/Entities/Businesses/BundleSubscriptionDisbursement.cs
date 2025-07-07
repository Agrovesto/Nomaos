using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class BundleSubscriptionDisbursement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string FarmerUserId { get; set; }
        public string FarmerName { get; set; }
        public long InterestedBundleId { get; set; }
        public string InterestedBundleName { get; set; }
        public long TotalQuantityPaidFor { get; set; }
        public long TotalQuantityReceived { get; set; }
        public DateTimeOffset DisbursedOn { get; set; }
        public string? Reference { get; set; }
        public string OfficerInChargeUserId { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string Location { get; set; }
        public long CooperativeId { get; set; }
        public string DisbursedBy { get; set; }
        public string PaymentStatus { get; set; }
        public bool InputAlreadyCollected { get; set; }
        public string InputCollectedByPictureOf { get; set; } //i.e image of who collected the input
        public string? OrganizationCode { get; set; }
    }
}