using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    
    public class FarmVisitationActivityModel
    { 
        public long Id { get; set; }
        public long FarmActivityId { get; set; }
        public string FarmerUserId { get; set; }
        public string? FarmerName { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string? OfficerInChargeId { get; set; }
        public string? CreatedBy { get; set; }
        public string? OfficerName { get; set; }
        public long CooperativeId { get; set; }
        public string? BelongToBusinessCode { get; set; }
        public string? ActivityType { get; set; }
        public decimal FarmerTransportCost { get; set; }
        public decimal FarmerLabourCost { get; set; }
        public decimal InputCost { get; set; } //input can be seed, water, fertilizer
        public string? InputType { get; set; }
        public double InputQuantity { get; set; }
        public decimal OtherCost { get; set; }
        public decimal TotalCost { get; set; }

        public string? SupportingImageUrl { get; set; }
    }

    public class FarmActivityTypeModel
    {
       
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string? BelongToBusinessCode { get; set; }
    }
}
