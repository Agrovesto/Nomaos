using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IDashboardManager
    {
        Task<Response<FarmerSalesSummary>> GetFarmerDashboardSummaryAsync(ListQueryParams query);
    }
}
