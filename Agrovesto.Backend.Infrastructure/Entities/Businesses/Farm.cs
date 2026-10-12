using Agrovesto.Backend.Core.Enums;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class Farm
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public double FarmSize { get; set; }
        public string? LocationDescription { get; set; } = string.Empty;

        // Ownership: Farmer / Cooperative / Community
        public string? OwnershipType { get; set; } = string.Empty;
        public long FarmerId { get; set; }
        //public long MembershipId { get; set; }
        //public long AgentId { get; set; }
        //public long CommunityId { get; set; }
        public string? MappedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string? CreatedBy { get; set; } = string.Empty;
        public string? FarmLocation { get; set; }
        public string? FarmState { get; set; }

        public string? FarmLga { get; set; }
        public string? FarmWard { get; set; }
        public double FarmSizeInDegress { get; set; }
        public double FarmSizeInSqMetre { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public string? MappingAccuracy { get; set; }
        public double DistanceCovered { get; set; } // in meters
        public double PinnedLongitude { get; set; }
        public double PinnedLatitude { get; set; }
        public ICollection<FarmMappingAuditTrail> AuditTrails { get; set; } = new List<FarmMappingAuditTrail>();

        public ICollection<FarmCoordinate> Coordinates { get; set; } = new List<FarmCoordinate>();
        public ICollection<FarmAllocation> Allocations { get; set; } = new List<FarmAllocation>(); // NEW - track who cultivates this land
        public ICollection<FarmLandDocument> FarmLandDocuments { get; set; } = new List<FarmLandDocument>();
        public DateTimeOffset? CapturedOn { get; set; }
        public DateTimeOffset? MeasurementDate { get; set; }
        public string? CapturedBy { get; set; }
        public double SizeInHectares { get; set; }
        public string? MeasurementMode { get; set; } // Walking, bike
        public double SizeInKm { get; set; }
        public bool IsMappingVerified { get; set; } = false;
        public bool IsDocumentVerified { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public bool IsSizeVerified { get; set; } = false;
        public string? MappingFlagReason { get; set; }
        public bool IsMappingFlagged { get; set; } = false;
        public string? FarmSizeType { get; set; } //Sqm, //Kilometre //hectare, acre
        public MappingStatus MappingStatus { get; set; } = MappingStatus.UnMapped;
        public Polygon? FarmPolygon { get; set; } // to get areas in degress, FarmPolygon.Area


        public string? PrimaryCrop { get; set; }
    }
 
    public class FarmAllocation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long CooperativeId { get; set; }
        public string? FarmerUserid { get; set; }

        public string AllocatedToType { get; set; } // "Farmer" or "Cooperative"
      //  public string AllocatedToId { get; set; }

        public double AllocatedArea { get; set; } // hectares
        public string Season { get; set; }
        public string Status { get; set; } = "Pending";
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(Farm))]
        public long FarmId { get; set; }
        public Farm Farm { get; set; } = null;
    }

    public class FarmLandDocument
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public DateTimeOffset UploadedOn { get; set; }
        public long FarmId { get; set; }
        public Farm Farm { get; set; }
        public FarmLandDocumentType DocumentType { get; set; }
        public string? DocumentUrl { get; set; }
        public string? DocumentTitle { get; set; }
        public string? DocumentId { get; set; }
        public bool IsVerified { get; set; } = false;
    }
    public enum FarmLandDocumentType
    {
        LandDocument,
        LandOwnerShip,
        LandLease
    }
    public class FarmMappingAuditTrail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string? Action { get; set; } // e.g. "Flagged", "Unflagged"
        public string PerformedByUserId { get; set; }
        public string PerformedByName { get; set; }
        public DateTimeOffset PerformedOn { get; set; } = DateTimeOffset.UtcNow;
        public string Remarks { get; set; } // e.g. reason for flag/unflag

        [ForeignKey(nameof(Farm))]
        public long FarmId { get; set; }
        public Farm Farm { get; set; } = null;
    }
    public class FarmCoordinate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public DateTimeOffset? FlaggedOn { get; set; } //= DateTimeOffset.Now;      
        public bool IsSystemOverlap { get; set; } // clearer than IsFarmFlaggedBySystem
        public string? FlaggedReason { get; set; } // e.g. "Same as existing", "Overlapping with FarmerId: 202"
        public string? CoordinateOverlapWithFarmerIds { get; set; } // comma-separated list of FarmerIds

        // Unflagging audit
        public bool UnflagFarm { get; set; }
        public string? FarmUnflaggedByUserId { get; set; }
        public string? FarmUnflaggedByName { get; set; }
        public DateTimeOffset? UnflaggedOn { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public long Order { get; set; } // order of the coordinate in the polygon

        [ForeignKey(nameof(Farm))]
        public long FarmId { get; set; }
       // [JsonIgnore]
        public Farm Farm { get; set; } = null;
    }

}
