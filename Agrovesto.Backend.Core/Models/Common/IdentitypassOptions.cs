using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class IdentitypassOptions
    {
        public string Url { get; set; }
        public string Secret_key { get; set; }
        public string Public_Key { get; set; }
        public string App_Id { get; set; }
        public string IdentityPayUrl { get; set; }
        public string Preembleyurl { get; set; }
        public string preembleyPublicKey { get; set; }
        public string preembleySecretKey { get; set; }
        public string preembleyAppId { get; set; }
    }
    //public class AesOptions
    //{
    //    public string Key { get; set; }
    //    public string Iv { get; set; }
    //    public string Salt { get; set; }
    //}

}
