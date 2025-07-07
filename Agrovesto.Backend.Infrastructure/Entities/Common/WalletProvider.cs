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

}
