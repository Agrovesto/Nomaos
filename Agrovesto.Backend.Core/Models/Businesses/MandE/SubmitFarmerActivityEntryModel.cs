using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    public class CreateMandEActivityTypeModel
    {
        public string Name { get; set; }
        public string? Description { get; set; }
    }
    public class CreateMandESubActivityModel
    {
        
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    public class CreateMandEFieldDefinitionModel
    {

        public long Id { get; set; }
        public long MandESubActivityId { get; set; }
        public string Label { get; set; }
        public string Key { get; set; }
        public string Type { get; set; } = "text"; // text, number, date, select, checkbox, textarea
        public string[]? Options { get; set; }
        public string? OptionsJson { get; set; }
        public bool IsRequired { get; set; }
        public long Order { get; set; } = 0;
        public string? BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }

    }
}
