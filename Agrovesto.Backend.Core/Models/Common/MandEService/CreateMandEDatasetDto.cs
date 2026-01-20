using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common.MandEService
{
    //POST /api/m-and-e/datasets/{datasetId}/records/bulk

    public class BulkUploadResultDto
    {
        public int Total { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class ChartDataPoint
    {
        public string X { get; set; }
        public decimal Y { get; set; }
    }


    //POST /api/m-and-e/datasets

    public class CreateMandEDatasetDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? OwnerType { get; set; }
    }

    //POST /api/m-and-e/datasets/{datasetId}/fields
//validation rules IsComputer == true , then formula is required
//key unique per dataset
//formula keys must exist in same dataset
    public class CreateMandEFieldDto
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string Type { get; set; }        // number, text, date
        public bool IsComputed { get; set; }
        public string? Formula { get; set; }
        public long? ReferenceDatasetId { get; set; }
        public string[]? Options { get; set; }
        public bool IsRequired { get; set; }
        public int Order { get; set; }
    }


    //POST /api/m-and-e/datasets/{datasetId}/records
    public class CreateMandERecordDto
    {
        public Dictionary<string, object> Data { get; set; }
    }
    public class UpdateMandERecordDto
    {
        public Dictionary<string, object> Data { get; set; }
    }
    //POST /api/m-and-e/datasets/{datasetId}/charts
    public class CreateMandEChartDto
    {
        public string ChartType { get; set; }   // line, bar
        public string XKey { get; set; }
        public string YKey { get; set; }
        public string? GroupByKey { get; set; }
    }

}
