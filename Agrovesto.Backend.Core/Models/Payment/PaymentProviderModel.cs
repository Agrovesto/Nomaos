using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment
{
    public class PaymentProviderModel : Model
    {
        public long ProviderId { get; set; }
        [Required(ErrorMessage = "Provider code is required")]
        [MaxLength(100)]
        public string Code { get; set; }
        [Required(ErrorMessage = "Provider name is required")]
        [MaxLength(100)]
        public string Name { get; set; }
        public ProviderStatus Status { get; set; } = ProviderStatus.Active;
        public List<ProviderParameterModel> PaymentProviderParameters { get; set; } = new List<ProviderParameterModel>();

    }
    public class ProviderParameterModel : Model
    {
        public long ParameterId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public long ProviderId { get; set; }
        public PaymentProviderModel PaymentProvider { get; set; }
        public Env Env { get; set; } = Env.Demo;
        public ParameterStatus ParamStatus { get; set; }
    }

    public enum ParameterStatus
    {
        Update, Create, Remove
    }
    public enum ProviderStatus
    {
        Active, Suspended, Disabled
    }
}
