using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class FarmerSalesSummaryDto
    {
        public decimal TotalVolumeSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageSalesValue { get; set; }
    }
    public class  FarmerSalesHistoryModel
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public long FarmerId { get; set; }
        public string ProductSold { get; set; }
        public double Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }

    }
    public class FarmerSalesTrackerRequestModel
    {
        public List<FarmerSalesTrackerModel> request {  get; set; }
    }
    public class FarmerSalesTrackerModel
    {

        public DateTimeOffset TransactionActualDate { get; set; }
        public long Id { get; set; }
        public string? CollectionOfficerId { get; set; }
        public DateTimeOffset CollectionDate { get; set; } = DateTimeOffset.Now;
        public string? MarketName { get; set; } //if on the platform, then Agrovesto Market
        public string? MarketDay { get; set; } //if on the platform, then Agrovesto Market
        public string? MarketLocation { get; set; } //if online, the online
        public string? MarketState { get; set; }
        public string? MarketLga { get; set; }
        public string? FarmerUserId { get; set; }
        public string CropSold { get; set; }
        public double QuantityBroughtToMarket { get; set; } //same as quantity uploaded on the platform
        public double QuantitySoldToday { get; set; }
        public UnitType UnitType { get; set; } //bag, modul, cup
        public string? UnitTypeName { get; set; } //bag, modul, cup
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
        public SalesType SalesType { get; set; }
        public string? SalesTypeName { get; set; }
        public string? BelongToBusinessCode { get; set; }
        public string? supervisorId { get; set; }
        public UnitType SoldCropUnitType { get; set; }
        public string? SoldCropUnitTypeName { get; set; }
        public decimal PriceOfLowestUnit { get; set; }
        public string? FarmerName { get; set; }
        public string? OrganizationCode { get; set; }
        public double LowestUnitQuntity { get; set; }
    }
    public enum SalesType
    {
        Offline,
        Online
    }
  
    public enum UnitType
    {
        Bag,
        Mudus,
        Tons,
        Kg,
        Others

    }
}
