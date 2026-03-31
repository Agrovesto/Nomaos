using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.V2.Sales
{
    public class FarmerSalesAnalyticsModel
    {
        public string Period { get; set; }   // e.g. "2026-03-30" or "Week 12"
        public decimal TotalSales { get; set; }
        public double TotalQuantity { get; set; }
        public int Transactions { get; set; }
    }
}
