using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    namespace Agrovesto.Application.DTOs.Farms
    {
        public class CreateFarmDto
        {
            public string FarmName { get; set; } = string.Empty;
            public string OwnershipType { get; set; } = string.Empty; // "Farmer", "Cooperative", "Community"
            public string? FarmerUserId { get; set; }
            public long CooperativeId { get; set; } // only populated if OwnerType = "Cooperative"
            public long CommunityId { get; set; } 

            public decimal TotalArea { get; set; } // e.g., 10 hectares
            public string LocationDescription { get; set; } = string.Empty;
            public List<FarmCoordinateDto> Coordinates { get; set; } = new();

            // Optional mapping attributes
            public bool IsVerified { get; set; } = false;
            public string? VerifiedById { get; set; } // Agent/Admin who verified
            public string FarmLocation { get; set; }
            public string FarmState { get; set; }
            public string FarmLga { get; set; }
            public string FarmWard { get; set; }
            public string? CoordinateTakenBy { get; set; }
            public string? CoordinateTakenByUserId { get; set; }
 
            public string? BelongToBusinessCode { get; set; }
            public string? OrganizationCode { get; set; }

        }
    }

    public class FarmDto
    {
        public string Name { get; set; } = string.Empty;
        public string OwnerShipType { get; set; } = string.Empty;// "Community" "Farmer" | "Cooperative" | "Community"
        public string? FarmerUserId { get; set; }
        public long CooperativeId { get; set; } // only populated if OwnerType = "Cooperative"
        public long CommunityId { get; set; } // only populated if OwnerType = "Community"
        public string LocationDescription { get; set; } = string.Empty;
        public List<FarmCoordinateDto> Coordinates { get; set; } = new();
    }
    public class FarmCoordinateDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public long Order { get; set; }
    }
    public class FarmAllocationDto
    {
        public long FarmId { get; set; }
        public long CooperativeId { get; set; }
        public string? FarmerUserId { get; set; }

        public string AllocatedToType { get; set; } // "Farmer" or "Cooperative"
       // public long AllocatedToId { get; set; }
        public double AllocatedArea { get; set; }
        public string Season { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

}
