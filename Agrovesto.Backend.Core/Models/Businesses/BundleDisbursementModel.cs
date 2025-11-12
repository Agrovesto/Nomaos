using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Agrovesto.Backend.Core.Models.Businesses
{
  
    public class BundleDisbursementModel
    {
        public IFormFile? FarmerImage { get; set; }
        public long Id { get; set; }
        public string FarmerUserId { get; set; }
        public string? FarmerName { get; set; }
        public long InterestedBundleId { get; set; }
        public string? InterestedBundleName { get; set; }
        public long TotalQuantityPaidFor { get; set; }
        public DateTimeOffset DisbursedOn { get; set; } = DateTimeOffset.UtcNow;
        public string? Reference { get; set; }
        public string? OfficerInChargeUserId { get; set; }
        public string? BelongToBusinessCode { get; set; }
        public string? Location { get; set; }
        public long CooperativeId { get; set; }
        public string? DisbursedBy { get; set; }
        public bool InputAlreadyCollected { get; set; }
        public string? InputCollectedByPictureOf { get; set; }
        public string? OrganizationCode { get; set; }
    }
}
