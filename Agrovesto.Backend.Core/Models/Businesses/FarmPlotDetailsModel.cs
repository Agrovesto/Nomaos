using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
  
    public class FarmPlotDetailsModel
    {
        public long Id { get; set; }
        public long CooperativeId { get; set; }
        public long FarmerId { get; set; }
        public string? FarmerName { get; set; }
        public double Latitude1 { get; set; }
        public double Longitude1 { get; set; }
        public double Latitude2 { get; set; }
        public double Longitude2 { get; set; }
        public double Latitude3 { get; set; }
        public double Longitude3 { get; set; }
        public double Latitude4 { get; set; }
        public double Longitude4 { get; set; }
        public string FarmLocation { get; set; }
        public string FarmState { get; set; }
        public string FarmLga { get; set; }
        public string FarmWard { get; set; }
        public string? CoordinateTakenBy {  get; set; }
        public string? CoordinateTakenByUserId { get; set; }
        public double AreaInDegress { get; set; }
        public double AreaInSqMetre { get; set; }
        public double AreaInHectare { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string? BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        // Flagging logic
        public bool IsFarmFlaggedBySystem { get; set; }
        public string? FlaggedReason { get; set; } // e.g. "Same as existing", "Overlapping with FarmerId: 202"
        public string? CoordinateOverlapWithFarmerIds { get; set; } // comma-separated list of FarmerIds

        // Unflagging audit
        public bool UnflagFarm { get; set; }
        public string? FarmUnflaggedByUserId { get; set; }
        public string? FarmUnflaggedByName { get; set; }
        public string? CooperativeName { get; set; }
        public DateTimeOffset? UnflaggedOn { get; set; }
        public List<FarmFlagAuditTrailModel> AuditTrails { get; set; } = new List<FarmFlagAuditTrailModel>();

    }
    public class FarmFlagAuditTrailModel
    {
        public long Id { get; set; }
        public long FarmPlotDetailsId { get; set; }
        public string Action { get; set; } // e.g. "Flagged", "Unflagged"
        public string PerformedByUserId { get; set; }
        public string PerformedByName { get; set; }
        public DateTimeOffset PerformedOn { get; set; } = DateTimeOffset.UtcNow;
        public string Remarks { get; set; } // e.g. reason for flag/unflag
    }

}
