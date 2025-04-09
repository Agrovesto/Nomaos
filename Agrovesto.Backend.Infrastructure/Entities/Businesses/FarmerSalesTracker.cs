using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class FarmerSalesTracker
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string MarketDay { get; set; }
        public string CollectionOfficerId { get; set; }
        public DateTimeOffset CollectionDate { get; set; }
        public DateTimeOffset TransactionActualDate { get; set; }
        public string MarketName { get; set; } //if on the platform, then Agrovesto Market
        public string MarketLocation { get; set; } //if online, the online
        public string? MarketState { get; set; }
        public string? MarketLga { get; set; }
        public string FarmerUserId { get; set; }
        public string CropSold { get; set; }
        public long QuantityBroughtToMarket { get; set; } //same as quantity uploaded on the platform
        public long QuantitySoldToday { get; set; }
        public string UnitType { get; set; }
        public decimal CropPricePerUnitSold { get; set; }
        public decimal TotalPriceSold { get; set; }
        public decimal IntendedPriceToSellPerUnit { get; set; }
        public bool IsCropTransportedToMarket { get; set; }
        public string? MeansOfTransport { get; set; }
        public decimal TransportCost { get; set; }
        public bool IsCropPackaged { get; set; }
        public string? PackagingType { get; set; } //sack, ziplock ect
        public decimal CostOfPackagingMaterial { get; set; }
        public decimal OtherCost { get; set; }
        public string ModeOfPayment { get; set; } //cash, transfer, pos etc
        public long TotalTimeSpentInMarket { get; set; }
        public string SalesType { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string? supervisorId { get; set; }
    }
}
