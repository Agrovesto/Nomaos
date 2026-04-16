using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agrovesto.Backend.Infrastructure.Entities.Common;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class Organization
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OrganizationId { get; set; }
        public string BusinessCode { get; set; }
        public string UserId { get; set; }
        public string OrganizationCode { get; set; }
        public string CompanyName { get; set;}
        public string? FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ContactEmail { get; set; }
        public string? OrganizationEmail { get; set; }
        public string? OrganizationPhone { get; set; }
        public long? StateId { get; set; }
        public States? State { get; set; }
        public long? LgaId { get; set; }
        public LGA? LGA { get; set; }
        public string? CompanyAddress { get; set; }

        public ICollection<OrganizationConfiguration> OrganizationConfigurations { get; set; } = new List<OrganizationConfiguration>();
    //    public ICollection<OrganizationBundlePlans> OrganizationBundlePlans { get; set; } = new List<OrganizationBundlePlans>();
    
        public string? CompanyType { get; set; } //Business, llc, public limited, ngo
        public string? CACNumber { get; set; }
        public string? RoleInOrganization { get; set; }
        public DateTimeOffset OnboardedOn { get; set; }// = DateTimeOffset.Now;
        public bool IsActive { get; set; } = true;

    }

}
