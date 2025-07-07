using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common.Dashboard
{
    internal class FarmerDashboard
    {
    }
    public class FarmerSalesSummary
    {
        public long TotalTransactions { get; set; }
        public decimal TotalIncome { get; set; }
        public List<FarmerSalesTrackerModel> Transactions { get; set; } = new();
        public List<SalesPerformanceDto> SalesPerformance { get; set; } = new();
        public List<TopCropDto> TopCrops { get; set; } = new();
    }

    public class SalesPerformanceDto
    {
        public string Crop { get; set; }
        public DateTime Date { get; set; }
        public double TotalQuantity { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class TopCropDto
    {
        public string Crop { get; set; }
        public decimal Price { get; set; }
        public double QuantityBrought { get; set; }
        public double QuantitySold { get; set; }

    }

}
