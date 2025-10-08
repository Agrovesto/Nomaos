using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class Organization
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OrganizationId { get; set; }
        public string BusinessCode { get; set; }
        public string OrganizationCode { get; set; }
        public string OrganizationName { get; set;}
        public string? ContacPerson { get; set; }
        public string? Contact { get; set; }
        public string? ContactEmail { get; set; }
        public string? OrganizationEmail { get; set; }
        public string? Location { get; set; }

        public ICollection<OrganizationConfiguration> OrganizationConfigurations { get; set; } = new List<OrganizationConfiguration>();
    //    public ICollection<OrganizationBundlePlans> OrganizationBundlePlans { get; set; } = new List<OrganizationBundlePlans>();
    
}
}
