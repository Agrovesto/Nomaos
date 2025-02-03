using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
  

    public class SterlingBankWallet
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public string AgrovestoUserId { get; set; }
        public string AccountNumber { get; set; }
        public string LedgerAccountNumber { get; set; }
        public string WalletId { get; set; }
        public string InternalAccountId { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
    }
}
