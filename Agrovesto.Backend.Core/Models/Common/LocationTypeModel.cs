using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class LocationTypeModel
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }

        public string? BusinessCode { get; set; }
    }

    public class OrganizationLocationModel
    {

        public long Id { get; set; }
        public long OrganizationId { get; set; }
        public string LocationName { get; set; }
        public long LocationTypeId { get; set; }
        public long StateId { get; set; }
        public long LgaId { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public string? BusinessCode { get; set; }
    }
}
