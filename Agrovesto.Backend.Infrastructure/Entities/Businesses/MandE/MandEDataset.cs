using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses.MandE
{
    /**
     * Dataset (Sheet)

Columns (Fields)

Rows (Records)

Formulas (Computed columns)

Views (Charts / pivots)

    One dataset = one Excel sheet
     * */
    public class MandEDataset 
    {
        public long Id { get; set; }
        public string Name { get; set; }          // e.g. "Input Sales Tracking"
        public string? Description { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public string? OwnerId { get; set; }
        public MandEOwnerType MandEOwnerType { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public ICollection<MandEDataFieldDefinition> Fields { get; set; } = new List<MandEDataFieldDefinition>();
        public ICollection<MandERecord> Records { get; set; } = new List<MandERecord>();

    }

    public enum MandEOwnerType
    {
        Individual=0,
        Organization= 1,
        Business=2,
        External= 3


    }
    public enum FieldType
    {
        Number,
        Text,
        Date,
        Boolean,
        Select,
        Reference
    }

    //2. Columns = Fields (extend what you already have)
    public class MandEDataFieldDefinition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long MandEDatasetId { get; set; }
        public MandEDataset MandEDataset { get; set; }
        public string Name { get; set; }      // Human name
        public string Key { get; set; }       // price, unit, total
        public string Type { get; set; }      // number, text, date
        public bool IsComputed { get; set; }

        public string? Formula { get; set; }  // e.g. "price * unit"
        public string[]? Options { get; set; }  // e.g. "price * unit"

        public long Order { get; set; }
       
       // public long MandESubActivityId { get; set; }
        public string Label { get; set; }
        public bool IsRequired { get; set; }
        public string? BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
    }
    public class MandERecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long MandEDatasetId { get; set; }
        public MandEDataset MandEDataset { get; set; }
        // one row of data
        public string DataJson { get; set; } = "{}";

        public DateTimeOffset CreatedOn { get; set; }
        public string CreatedByUserId { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }

    }
    public class MandEChartDefinition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long MandEDatasetId { get; set; }
        public MandEDataset MandEDataset { get; set; }
        public string ChartType { get; set; } // line, bar, pie
        public string XKey { get; set; }      // "date"
        public string YKey { get; set; }      // "total"
        public string? GroupByKey { get; set; } // "name"
    }
   
    public class MandEDatasetPermission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long MandEDatasetId { get; set; }
        public string UserId { get; set; }
        public string Role { get; set; } // Designer, Editor, Viewer
    }
}
