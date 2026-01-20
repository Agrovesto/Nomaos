using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common.MandEService
{
    //GET /api/m-and-e/datasets
//filter by businessCode and/or organizationCode
    public record MandEDatasetDto(
      long Id,
      string Name,
      string? Description,
      long fieldCount,
      long recordCount,
      bool isLocked
  );// DatasetOverviewDto.cs
    public class DatasetOverviewDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsLocked { get; set; }

        public DatasetStatsDto Stats { get; set; } = null!;
        public List<DatasetValidationDto> Validations { get; set; } = new();
        public List<DatasetRecordDto> RecentRecords { get; set; } = new();
        public List<DatasetActivityDto> ActivityLog { get; set; } = new();
        public long RecordCount { get; set; }
        public long FieldCount { get; set; }
    }

    public class DatasetStatsDto
    {
        public long RecordCount { get; set; }
        public long FieldCount { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
        public double CompletenessPercentage { get; set; } // 0-100
    }

    public class DatasetValidationDto
    {
        public string FieldName { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsCritical { get; set; }
    }

    public class DatasetRecordDto
    {
        public long Id { get; set; }
        public Dictionary<string, object> Values { get; set; } = new();
        public DateTimeOffset CreatedOn { get; set; }
    }

    public class DatasetActivityDto
    {
        public DateTimeOffset Timestamp { get; set; }
        public string User { get; set; } = null!;
        public string Action { get; set; } = null!;
    }

    public class MandEDatasetListDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }

        public long FieldCount { get; set; }
        public long RecordCount { get; set; }

        public bool IsLocked => RecordCount > 0;
    }
    //GET /api/m-and-e/datasets/{datasetId}/fields
    public record MandEFieldDto(
    long Id,
    string Name,
    string Key,
    string Type,
    bool IsComputed,
    string? Formula,
    bool IsRequired,
    long Order,
    string[] options,
    string label,
    string DatasetName,
    long DatasetId
);
    //GET /api/m-and-e/datasets/{datasetId}/records
    //allows for pagination, date filtering, export later

    public record MandERecordDto(
    long Id,
    Dictionary<string, object> Data,
    DateTimeOffset CreatedOn
);

    //GET /api/m-and-e/datasets/{datasetId}/charts

}
