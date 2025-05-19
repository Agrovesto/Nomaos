using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common.Dashboard;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly DataContext _context;
        public DashboardRepository(DataContext context)
        {
            _context = context;

        }

        #region dashboard
        public async Task<FarmerSalesSummary> GetFarmerDashboardSummaryAsync(string farmerUserId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            // Default to current year range if no dates are supplied
            if (startDate == null || endDate == null)
            {
                var now = DateTimeOffset.UtcNow;
                startDate ??= new DateTimeOffset(new DateTime(now.Year, 1, 1), TimeSpan.Zero);
                endDate ??= new DateTimeOffset(new DateTime(now.Year, 12, 31, 23, 59, 59), TimeSpan.Zero);
            }

            // Base query for this farmer, filtered by date range
            var query = _context.FarmerSalesTracker
                .Where(f => f.FarmerUserId == farmerUserId &&
                            f.TransactionActualDate >= startDate &&
                            f.TransactionActualDate <= endDate);

            // 1. Total Transactions
            var totalTransactionsTask = await query.CountAsync();

            // 2. Transactions List
            var transactionsTask = await query
                .OrderByDescending(f => f.TransactionActualDate)
                .ToListAsync();

            // 3. Sales Performance
            var salesPerformanceTask = await query
                .GroupBy(f => new
                {
                    f.CropSold,
                    SalesDate = f.TransactionActualDate.Date
                })
                .Select(g => new SalesPerformanceDto
                {
                    Crop = g.Key.CropSold,
                    Date = g.Key.SalesDate,
                    TotalQuantity = g.Sum(x => x.QuantitySoldToday),
                    TotalSales = g.Sum(x => x.TotalPriceSold)
                })
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            // 4. Top Crops
            var topCropsTask = await query
                .GroupBy(f => f.CropSold)
                .Select(g => new TopCropDto
                {
                    Crop = g.Key,
                    Price = g.Average(x => x.TotalPriceSold), // Or pick another logic here
                    QuantityBrought = (long)g.Sum(x => x.QuantityBroughtToMarket),
                    QuantitySold = (long)g.Sum(x => x.QuantitySoldToday)
                })
                .OrderByDescending(g => g.QuantitySold)
                .Take(3)
                .ToListAsync();

            // 5. Return Summary
            return new FarmerSalesSummary
            {
                TotalTransactions = totalTransactionsTask,
                TotalIncome = transactionsTask.Sum(x=>x.TotalPriceSold),
                Transactions = transactionsTask.Select(x => x.Map()).ToList(),
                SalesPerformance = salesPerformanceTask,
                TopCrops = topCropsTask
            };
        }

        #endregion dashboard
    }
}
