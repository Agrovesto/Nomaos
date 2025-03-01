using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class WalletProviderModel : Model
    {
        //[Key]
        public long ProviderId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public ProviderStatus Status { get; set; } = ProviderStatus.Active;

        public List<WalletProviderParameterModel> WalletProviderParameters { get; set; }

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
    public enum ProviderStatus
    {
        Active, Suspended, Disabled
    }
    public enum Env
    {
        Demo, Live
    }
}
