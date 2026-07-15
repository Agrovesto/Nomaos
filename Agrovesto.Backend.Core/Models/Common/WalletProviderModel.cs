using Agrovesto.Backend.Core.Models.Payment;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class BusinessWalletProviderModel : Model
    {
        public long BusinessWalletProviderId { get; set; } = 0;
        public string BusinessCode { get; set; }
        public long ProviderId { get; set; }
        public ProviderStatus Status { get; set; } = ProviderStatus.Active;
        public bool IsDefault { get; set; }

        // Populated on read via .Include(x => x.WalletProvider) — useful for listing
        // "providers enabled for this business" with name/code without a second call.
        public WalletProviderModel WalletProvider { get; set; }

        public List<BusinessWalletProviderParameterModel> Parameters { get; set; } = new();
    }

    public class BusinessWalletProviderParameterModel
    {
        public long ParameterId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public long BusinessWalletProviderId { get; set; }
        public Env Env { get; set; } = Env.Demo;
    }
    public class WalletProviderModel : Model
    {
        //[Key]
        public long ProviderId { get; set; } = 0;
        public string Code { get; set; }
        public string Name { get; set; }
        public ProviderStatus Status { get; set; } = ProviderStatus.Active;

        public List<WalletProviderParameterModel> WalletProviderParameters { get; set; } = new();

    }
    public class WalletProviderParameterModel
    {
       // [Key]
        public long ParameterId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public long ProviderId { get; set; }
      //  public WalletProviderModel WalletProvider { get; set; }
        public Env Env { get; set; } = Env.Demo;

       // public ParameterStatus ParamStatus { get; set; }

    }
    public enum ParameterStatus
    {
        Update, Create, Remove
    }

    public enum Env
    {
        Demo, Live
    }
}
