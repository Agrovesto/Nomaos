using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models
{
    public class TenantContext
    {
        public string UserId { get; set; }

        public long BusinessId { get; set; }

        public string BusinessCode { get; set; }

        public string Role { get; set; }

        public bool IsLicenseValid { get; set; }

        public DateTimeOffset LicenseExpiry { get; set; }

        public List<string> Modules { get; set; } = new();

        public string? ThemeColor { get; set; }

        public string? LogoUrl { get; set; }
    }
}
