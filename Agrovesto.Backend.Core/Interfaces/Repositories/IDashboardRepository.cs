using Agrovesto.Backend.Core.Models.Common.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IDashboardRepository
    {
        Task<FarmerSalesSummary> GetFarmerDashboardSummaryAsync(string farmerUserId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
    }
}
