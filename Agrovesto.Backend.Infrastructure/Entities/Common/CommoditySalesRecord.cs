using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class Commodity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public string Name { get; set; } // Maize, Rice, Sorghum

        public long CommodityCategoryId { get; set; }
        public CommodityCategory Category { get; set; }

        public StandardUnit StandardUnit { get; set; } // Base unit (KG)
        public CommodityGrade CommodityGrade { get; set; } // Base unit (KG)

        public ICollection<CommodityVariety> Varieties { get; set; } = new List<CommodityVariety>();
    }
    public enum CommodityGrade
    {
        A = 1,
        B = 2,
        C = 3
    }

    public enum StandardUnit
    {
        Kilogram = 1,
        Tonne = 2,
        Bag = 3
    }

    public class CommodityVariety
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long CommodityId { get; set; }
        public Commodity Commodity { get; set; }

        public string Name { get; set; } // Local Rice, Faro 44, Yellow Maize
    }

    public class CommodityCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public string Name { get; set; } // Grains, Tubers, Legumes, Cash Crops

        public ICollection<Commodity> Commodities { get; set; } = new List<Commodity>();
    }

    public class MarketTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        /// <summary>
        /// commodity
        /// </summary>
        ///
        public long CommodityId { get; set; }
        public Commodity Commodity { get; set; }
        public long? CommodityVarietyId { get; set; }
        public CommodityVariety CommodityVariety { get; set; }
        public CommodityGrade Grade { get; set; }
        public decimal? MoistureContent { get; set; }


        /* =======================
       Market Context
       ======================= */
        public long MarketId { get; set; }
        public Market Market { get; set; }
        public long MarketDayId { get; set; }
        public MarketDay MarketDay { get; set; }
        public DateTimeOffset SalesDate { get; set; }

        /* =======================
       Quantity & Pricing
       ======================= */
        public decimal QuantityKg { get; set; }
        public decimal PricePerKg { get; set; }

        public decimal TotalAmount => QuantityKg * PricePerKg;


        /* =======================
      Actors
      ======================= */
        public string SellerId { get; set; }
        public CommodityHolderType SellerType { get; set; }

        public string BuyerId { get; set; }
        public BuyerType BuyerType { get; set; }

        public SalesMode SalesMode { get; set; }

        public string? FacilitatorId { get; set; }
        public decimal? FacilitatorFee { get; set; }
        public decimal? CommissionRate { get; set; }


        /* =======================
       Payment
       ======================= */
        public PaymentMethod PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }

        /* =======================
      Metadata
      ======================= */
        public TransactionSource Source { get; set; } // Agent, Warehouse, Trader
        public TransactionStatus Status { get; set; }

        public string RecordedByUserId { get; set; }
        public DateTimeOffset CreatedOn { get; set; }

        public string BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }


       
        public DateTimeOffset CreateOn { get; set; } //date it was logged
      
      
      //  public bool IsCommodityPackaged { get; set; }
        //public string? PackagingType { get; set; } //sack, ziplock ect
        //public decimal CostOfPackagingMaterial { get; set; }
        //public decimal OtherCost { get; set; }
        
    }

    public class TransactionCost
    {
        public long Id { get; set; }
        public long MarketTransactionId { get; set; }

        public CostType CostType { get; set; }
        public decimal Amount { get; set; }
    }
    public enum CostType
    {
        Packaging,
        Transport,
        Levies,
        AgentFees
    }

    public enum SalesMode
    {
        Direct = 1,
        Facilitated = 2
    }

    public enum PaymentMethod
    {
        Cash = 1,
        Transfer = 2,
        POS = 3,
        Credit = 4
    }

    public enum TransactionSource
    {
        Agent = 1,
        Warehouse = 2,
        Trader = 3
    }

    public enum TransactionStatus
    {
        Pending = 1,
        Verified = 2,
        Disputed = 3
    }

 
     public enum BuyerType
    {
        Individual,
        Trader,
        Processor,
        Exporter,
        Institution
    }
    public enum CommodityHolderType
    {
        Self,
        Farmer,
        Cooperative,
        Organization,
        IndividualTrader //individual seller
    }

    public class Market
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public string Name { get; set; }

        public MarketType MarketType { get; set; }

        // Location (normalized)
        public long StateId { get; set; }
        public States State { get; set; }

        public long LgaId { get; set; }
        public LGA Lga { get; set; }

        // Geo data
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Estimated daily/market-day volume (kg)
        public decimal EstimatedCapacityKg { get; set; }

        // Relationships
        public ICollection<MarketDay>? MarketDays { get; set; } = new List<MarketDay>();
        public ICollection<MarketCommodity>? MarketCommodities { get; set; } = new List<MarketCommodity>();

        public string Location { get; set; }
        // Audit
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
    }
    public class MarketCommodity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long MarketId { get; set; }
        public Market Market { get; set; }

        public long CommodityId { get; set; }
        public Commodity Commodity { get; set; }
    }

    public class MarketDay
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long MarketId { get; set; }
        public Market Market { get; set; }

        public MarketDayType DayType { get; set; }
        public DayOfWeek? DayOfWeek { get; set; } // For weekly markets
        public int? CycleDay { get; set; }        // For 4-day cycles

        public bool IsActive { get; set; } = true;
    }
    public enum MarketType
    {
        Rural = 1,
        Urban = 2,
        AggregationHub = 3,
        BorderMarket = 4
    }

    public enum MarketDayType
    {
        Weekly = 1,
        Cycle = 2
    }

}
