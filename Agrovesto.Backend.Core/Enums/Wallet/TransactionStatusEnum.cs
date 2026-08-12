using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Enums.Wallet
{
    // Mirrors Infrastructure.Entities.Common.WalletTransactionStatus so Core can expose
    // it as a query parameter without referencing Infrastructure. The ordinals must stay
    // in step with that enum - the repository casts between the two.
    public enum WalletTransactionStatus
    {
        Initiated = 0,

        DebitPending = 1,
        DebitSuccessful = 2,
        DebitFailed = 3,

        CreditPending = 4,
        CreditSuccessful = 5,
        CreditFailed = 6,

        Completed = 7,

        ReconciliationRequired = 8,
        ReversalPending = 9,
        Reversed = 10,

        Failed = 11,
    }
}
