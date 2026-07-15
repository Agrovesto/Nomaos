using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class WalletProvider
    {
        [Key]
        public long ProviderId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public ICollection<WalletProviderParameter> WalletProviderParameters { get; set; } = new HashSet<WalletProviderParameter>();
        public ICollection<BusinessWalletProvider> BusinessWalletProviders { get; set; } = new HashSet<BusinessWalletProvider>();


    }
    public class WalletProviderParameter
    {
        [Key]
        public long ParameterId { get; set; }
        public string Value { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public long WalletProviderProviderId { get; set; }
        public WalletProvider WalletProvider { get; set; }
        public string Env { get; set; }

    }
    // Join entity: which providers a business has opted into
    public class BusinessWalletProvider
    {
        [Key]
        public long BusinessWalletProviderId { get; set; }
        public string BusinessCode { get; set; }
        public string Status { get; set; }     // Active/Inactive/Suspended for this tenant
        public bool IsDefault { get; set; }    // if a business enables multiple providers

        public long ProviderId { get; set; }
        public WalletProvider WalletProvider { get; set; }

        public ICollection<BusinessWalletProviderParameter> Parameters { get; set; } = new HashSet<BusinessWalletProviderParameter>();
    }

    // Tenant-specific overrides/credentials (API key, secret, merchant ID, etc.)
    public class BusinessWalletProviderParameter
    {
        [Key]
        public long ParameterId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Env { get; set; }

        public long BusinessWalletProviderId { get; set; }
        public BusinessWalletProvider BusinessWalletProvider { get; set; }
    }
}
