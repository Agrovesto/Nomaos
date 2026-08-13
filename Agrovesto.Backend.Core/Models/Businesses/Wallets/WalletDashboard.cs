using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses.Wallets
{
    public class WalletDashboardResponseModel
    {
        public decimal OutStandingInputDebt { get; set; }
        public decimal NextDuePaymentAmount { get; set; }
        // Null when the user has no input financing, so nothing is due.
        public DateTimeOffset? NextDuePaymentDate { get; set; }
        public decimal TotalWalletBalance { get; set; }

    }
}
