using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class Farm
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double AreaSize { get; set; }
        public string LocationDescription { get; set; } = string.Empty;

        // Ownership: Farmer / Cooperative / Community
        public string OwnershipType { get; set; } = string.Empty;
        public string? FarmerUserId { get; set; }
        public long CooperativeId { get; set; }
        public long CommunityId { get; set; }
        public string MappedBy { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;
        public string FarmLocation { get; set; }
        public string FarmState { get; set; }

        public string FarmLga { get; set; }
        public string FarmWard { get; set; }


        public string CoordinateTakenBy { get; set; }
        public string CoordinateTakenByUserId { get; set; }
        public double AreaInDegress { get; set; }
        public double AreaInSqMetre { get; set; }
        public string BelongToBusinessCode { get; set; }
        public DateTimeOffset FlaggedOn { get; set; } = DateTimeOffset.Now;
        public string? OrganizationCode { get; set; }
        public bool IsFarmFlaggedBySystem { get; set; }
        public string? FlaggedReason { get; set; } // e.g. "Same as existing", "Overlapping with FarmerId: 202"
        public string? CoordinateOverlapWithFarmerIds { get; set; } // comma-separated list of FarmerIds

        // Unflagging audit
        public bool UnflagFarm { get; set; }
        public string? FarmUnflaggedByUserId { get; set; }
        public string? FarmUnflaggedByName { get; set; }
        public DateTimeOffset? UnflaggedOn { get; set; }
        public ICollection<FarmMappingAuditTrail> AuditTrails { get; set; } = new List<FarmMappingAuditTrail>();

        public ICollection<FarmCoordinate> Coordinates { get; set; }
        public ICollection<FarmAllocation> Allocations { get; set; } // NEW - track who cultivates this land

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


    public class FarmMappingAuditTrail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Action { get; set; } // e.g. "Flagged", "Unflagged"
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
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public long Order { get; set; } // order of the coordinate in the polygon

        [ForeignKey(nameof(Farm))]
        public long FarmId { get; set; }
        public Farm Farm { get; set; } = null;
    }

}
