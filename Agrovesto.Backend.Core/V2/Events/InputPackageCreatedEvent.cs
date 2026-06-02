using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.V2.Events
{
    public class InputPackageCreatedEvent
    {
        public long PackageId { get; set; }
        public long OrganizationId { get; set; }
        public string CreatedByUserId { get; set; }
    }
    public class InputPackageConfigurationUpdatedEvent
    {
        public long PackageId { get; set; }
        public long OrganizationId { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }
}
