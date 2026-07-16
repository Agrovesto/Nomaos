using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Market
{
    public class TransactionTrustResult
    {
        public int Score { get; set; }
        public List<string> Flags { get; set; } = new();
    }

}
