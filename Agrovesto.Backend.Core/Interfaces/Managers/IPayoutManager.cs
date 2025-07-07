using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IPayoutManager
    {
        Task<(bool status, PayoutModel data)> WithdrawFund(PayoutModel model, BusinessRegistrationArgs args);
    }
}
