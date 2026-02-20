using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses.Wallets
{
    public class UserWalletParams
    {
        public string businessCode { get; set; }
        public string UserId { get; set; }
        public string Env { get; set; }
    }
    public class UserWalletModel
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public long WalletProviderId { get; set; }
        public decimal WalletBalance { get; set; } = 0;
        public string Currency { get; set; } = "NGN";
            public bool IsActive { get; set; } 
        public bool IsDefault { get; set; }
        public string WalletNumber { get; set; }
        public string ProviderName { get; set; }
        public byte[] RowVersion { get; set; }
    }

}
