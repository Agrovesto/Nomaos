using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IInputFinancingManagementRepository
    {
        Task<(bool status, BundleSubscriptionInterestModel data)> CreateInputFinancingInterestThroughBundle(BundleSubscriptionInterestModel data);
    }
}
