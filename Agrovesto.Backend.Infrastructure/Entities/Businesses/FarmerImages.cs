using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class FarmerImages
    {
        public long Id { get; set; }
        public string ImageName { get; set; }
        public string FarmerUserId { get; set; }
        public string IMageKey { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
    }
}
