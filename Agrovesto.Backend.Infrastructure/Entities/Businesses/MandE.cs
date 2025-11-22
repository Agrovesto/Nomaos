using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class MandE
    {
    }
    public class MandEActivityType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }

       // public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
    }
    public class MandESubActivity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long MandEActivityTypeId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }


    }
    public class MandEFieldDefinition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long MandESubActivityId { get; set; }
        public string Label { get; set; }
        public string Key { get; set; }
        public string Type { get; set; } = "text"; // text, number, date, select, checkbox, textarea
        public string? OptionsJson { get; set; }
        public bool IsRequired { get; set; }
        public long Order { get; set; } = 0;
        public string? BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }

    }
    public class MandEFarmActivity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string FarmerUserId { get; set; }
        public string FarmerName { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public long FarmPlotId { get; set; }
        public long MandEActivityTypeId { get; set; }
        public long MandESubActivityId { get; set; }
        public DateTimeOffset ActivityDate { get; set; }
        public string DataJson { get; set; } = "{}";
        public string? Notes { get; set; }
        public string PerformedByUserId { get; set; }
        public string? AttachmentUrl { get; set; }

        public DateTimeOffset CreatedOn {get; set; } = 
            DateTimeOffset.Now;
    }
}
