using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IInputFinancingManagement
    {
        Task<(bool status, BundleSubscriptionInterestModel data)> CreateInputFinancingInterest(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args);
    }
}
