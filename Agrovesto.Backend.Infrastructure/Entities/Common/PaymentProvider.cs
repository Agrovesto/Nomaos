using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class PaymentProvider
    {
        [Key]
        public long ProviderId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public ICollection<PaymentProviderParameter> PaymentProviderParameters { get; set; } = new HashSet<PaymentProviderParameter>();

    }
    public class PaymentProviderParameter
    {
        [Key]
        public long ParameterId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public long PaymentProviderProviderId { get; set; }
        public PaymentProvider PaymentProvider { get; set; }
        public string Env { get; set; }
    }
}
