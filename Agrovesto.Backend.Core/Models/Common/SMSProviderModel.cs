using Agrovesto.Backend.Core.Models.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{

    public class BusinessSMSProviderModel : Model
    {
        public long BusinessSMSProviderId { get; set; } = 0;
        public string BusinessCode { get; set; }
        public long ProviderId { get; set; }
        public ProviderStatus Status { get; set; } = ProviderStatus.Active;
        public bool IsDefault { get; set; }

        // Populated on read via .Include(x => x.SMSProvider) — useful for listing
        // "providers enabled for this business" with name/code without a second call.
        public SMSProviderModel SMSProvider { get; set; }

        public List<BusinessSMSProviderParameterModel> Parameters { get; set; } = new();
    }

    public class BusinessSMSProviderParameterModel
    {
        public long ParameterId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public long BusinessSMSProviderId { get; set; }
        public Env Env { get; set; } = Env.Demo;
    }
    public class SMSProviderModel : Model
    {
        //[Key]
        public long ProviderId { get; set; } = 0;
        public string Code { get; set; }
        public string Name { get; set; }
        public ProviderStatus Status { get; set; } = ProviderStatus.Active;

        public List<SMSProviderParameterModel> SMSProviderParameters { get; set; } = new();

    }
    public class SMSProviderParameterModel
    {
        // [Key]
        public long ParameterId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public long ProviderId { get; set; }
        //  public SMSProviderModel SMSProvider { get; set; }
        public Env Env { get; set; } = Env.Demo;

        // public ParameterStatus ParamStatus { get; set; }

    }

}
