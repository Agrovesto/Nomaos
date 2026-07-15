using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class SMSProvider
    {
        [Key]
        public long ProviderId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public ICollection<SMSProviderParameter> SMSProviderParameters { get; set; } = new HashSet<SMSProviderParameter>();
        public ICollection<BusinessSMSProvider> BusinessSMSProviders { get; set; } = new HashSet<BusinessSMSProvider>();


    }
    public class SMSProviderParameter
    {
        [Key]
        public long ParameterId { get; set; }
        public string Value { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public long SMSProviderProviderId { get; set; }
        public SMSProvider SMSProvider { get; set; }
        public string Env { get; set; }

    }
    // Join entity: which providers a business has opted into
    public class BusinessSMSProvider
    {
        [Key]
        public long BusinessSMSProviderId { get; set; }
        public string BusinessCode { get; set; }
        public string Status { get; set; }     // Active/Inactive/Suspended for this tenant
        public bool IsDefault { get; set; }    // if a business enables multiple providers

        public long ProviderId { get; set; }
        public SMSProvider SMSProvider { get; set; }

        public ICollection<BusinessSMSProviderParameter> Parameters { get; set; } = new HashSet<BusinessSMSProviderParameter>();
    }

    // Tenant-specific overrides/credentials (API key, secret, merchant ID, etc.)
    public class BusinessSMSProviderParameter
    {
        [Key]
        public long ParameterId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Env { get; set; }

        public long BusinessSMSProviderId { get; set; }
        public BusinessSMSProvider BusinessSMSProvider { get; set; }
    }
}
