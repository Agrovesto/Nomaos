using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class OrganizationModel
    {
        public long OrganizaionId { get; set; }
        public string? BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public string OrganizationName { get; set; }
        public string? ContacPerson { get; set; }
        public string? Contact { get; set; }
        public string? ContactEmail { get; set; }
        public string? OrganizationEmail { get; set; }
        public string? Location { get; set; }
        public string? Password { get; set; }
    }
}
