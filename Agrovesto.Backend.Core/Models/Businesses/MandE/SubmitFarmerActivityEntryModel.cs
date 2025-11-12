using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses.MandE
{
   
    // Application/DTOs/FieldDtos.cs
    public record MandEActivityTypeModel(long Id, string Name, string? Description, string BelongToBusinessCode, string? OrganizationCode);
    public record SubMandEActivityModel(long Id, long ActivityTypeId, string Name, string? Description, string BelongToBusinessCode, string? OrganizationCode);
    public record TrackableMandEFieldModel(long Id, long SubActivityId, string Label, string Key, string FieldType, bool IsRequired, string[]? Options, string BelongToBusinessCode, string? OrganizationCode);

    // Application/DTOs/SubmitEntryDto.cs
    public class SubmitFarmerActivityEntryModel
    {
        public string FarmerUserId { get; set; }
        public long FarmPlotId { get; set; }
        public long ActivityTypeId { get; set; }
        public long SubActivityId { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public Dictionary<string, object> FieldData { get; set; } = new();
        public string? AttachmentUrl { get; set; }

        public string BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
    }
    public class MandEActivityLogModel
    {
        public long Id { get; set; }

        public string FarmerUserId { get; set; }
        public long FarmPlotId { get; set; }
        public long ActivityTypeId { get; set; }
        public long SubActivityId { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Notes { get; set; }
        public string AttachmentUrl { get; set; }
        public Dictionary<string, object> FieldData { get; set; } = new();
        // Navigation (optional)
        //public Farmer Farmer { get; set; }
        //public FarmPlot FarmPlot { get; set; }
    }

}
