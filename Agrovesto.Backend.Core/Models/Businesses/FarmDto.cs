using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    namespace Agrovesto.Application.DTOs.Farms
    {
        public class FarmMappingArgs
        {
            public long farmerId { get; set; }
            public long agentId { get; set; }
            public long membershipId { get; set; }
            public string? userId { get; set; }
            public string? farmerUserId { get; set; }
            public string? googleDriveDocumentId { get; set; }
            public string? performedByUserId { get; set; }
            public string? performedByName { get; set; }
            public string? businessCode { get; set; }
            public string? organizationCode { get; set; }
            public long farmId { get; set; }
            public bool ContinueDraft { get; set; } = false;
            public string? MappingStatus { get; set; } 
            public string? DocumentStatus { get; set; }
        }
        public class FarmLandDocumentDto
        {
        
            public string? DocumentType { get; set; } // LandDocument,
            //LandOwnerShip,
            //LandLease
            public string? DocumentUrl { get; set; }
            public string? DocumentTitle { get; set; }
            public string Status { get; set; } // Pending, Approved, Rejected
            public DateTimeOffset UploadedOn { get; set; }
                public string UploadedBy { get; set; }
        }
        public class FarmDetailsResponse
        {
            public long FarmId { get; set; }
            public DateTimeOffset DateAdded { get; set; }
            public string AddedBy { get; set; }
            public string AddedById { get; set; }
            public string AddedByType { get; set; }
            public string MappingStatus { get; set; }
            public DateTimeOffset? CapturedOn { get; set; }
            public string CapturedBy { get; set; }
            public string CapturedByType { get; set; }
            public string CapturedById { get; set; }
            public string Accuracy { get; set; }
            public List<FarmLandDocumentDto> Documents { get; set; }
            public List<FarmCoordinateDto> Coordinates { get; set; }
            public LandMeasurementDetails MeasurementDetails { get; set; } 
        }
        public class LandMeasurementDetails
        {
            public string? MappingAccuracy { get; set; }
            public double PinnedLongitude { get; set; }
            public double PinnedLatitude { get; set; }
            public double MeasuredArea { get; set; }
            public double AreaUnit { get; set; } // e.g., hectares, acres
            public string MeasurementMode { get; set; } // e.g., GPS, Manual
            public double DistanceCovered { get; set; } // in meters
            public DateTimeOffset? MeasurementDate { get; set; }
        }
        public class FarmListRespopnseObject
        {
            public string FarmState { get; set; }
            public string FarmLga { get; set; }
            public string FarmWard { get; set; }
            public string FarmName { get; set; }
            public string FarmLocation { get; set; }
            
            public long FarmId { get; set; }
            public double LandSize { get; set; }
            public string DocumentStatus { get; set; }
            public string MappingStatus { get; set; }
            public DateTimeOffset DateAdded { get; set; }

        }
        public class SubmitFarmRequest
        {

            public string OwnershipType { get; set; } = "Farmer";
            public string FarmLocation { get; set; }
            public string? FarmName { get; set; }
            public string FarmState { get; set; }
            public string FarmLga { get; set; }
            public string FarmWard { get; set; }
            public double SizeInHectares { get; set; }
            public string? MeasurementMode { get; set; } // Walking, bike
            public double SizeInKm { get; set; }
            public string? MappingAccuracy { get; set; }
            public double DistanceCovered { get; set; } // in meters
            public double PinnedLongitude { get; set; }
            public double PinnedLatitude { get; set; }
            // Mapping
            public string? CoordinatesJson { get; set; }

            // Documents
            public List<IFormFile>? Documents { get; set; }
            public List<string>? DocumentTypes { get; set; }
        }
        public class MapFarmRequest
        {
            public string? MappingAccuracy { get; set; }
            public double DistanceCovered { get; set; } // in meters
            public double PinnedLongitude { get; set; }
            public double PinnedLatitude { get; set; }
            // Ownership: Farmer / Cooperative / Community
            public string? OwnershipType { get; set; } = string.Empty;
            public string? FarmName { get; set; } = string.Empty;
            public string FarmLocation { get; set; }
            public string FarmState { get; set; }
            public string FarmLga { get; set; }
            public string FarmWard { get; set; }
            public double AreaSize { get; set; }
            public string? BelongToBusinessCode { get; set; }
            public string? OrganizationCode { get; set; }
            public double SizeInHectares { get; set; }
            public string? MeasurementMode { get; set; } // Walking, bike
            public string? FarmSizeType { get; set; } // Walking, bike
            public double SizeInKm { get; set; }
            public List<FarmCoordinateDto> Coordinates { get; set; } = new();
            public List<FarmLandDocumentDto> Documents { get; set; } = new();  
        }

        public class FarmCoordinateDto
        {
            [JsonPropertyName("latitude")]
            public double Latitude { get; set; }

            [JsonPropertyName("longitude")]
            public double Longitude { get; set; }

            public long Order { get; set; }

        }

        public class FarmCoordinateRequest
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public long Order { get; set; }
        }

        public class FarmOverlapInfo
        {
            public long FarmId { get; set; }
            public long FarmerId { get; set; }
        }

        public class CreateFarmDto
        {
            public long Id { get; set; }
            public string FarmName { get; set; } = string.Empty;
            public string LocationDescription { get; set; } = string.Empty;

            public string OwnershipType { get; set; } = string.Empty; // "Farmer", "Cooperative", "Community"
            public long FarmerId { get; set; }
            public long CooperativeId { get; set; } // only populated if OwnerType = "Cooperative"
            public long CommunityId { get; set; }
            public string? CoordinateTakenBy { get; set; }
            public string? CoordinateTakenByUserId { get; set; }

            public decimal TotalArea { get; set; } // e.g., 10 hectares
            public List<FarmCoordinateDto> Coordinates { get; set; } = new();

            // Optional mapping attributes
            public bool IsVerified { get; set; } = false;
            public string? VerifiedById { get; set; } // Agent/Admin who verified
            public string FarmLocation { get; set; }
            public string FarmState { get; set; }
            public string FarmLga { get; set; }
            public string FarmWard { get; set; }
            
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
