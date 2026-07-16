using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Market
{
    public class MarketDto
    {
   
       public long Id { get; set; }
        [Required]
        public string Name { get; set; }

        public string MarketType { get; set; }

        public long StateId { get; set; }
        public string StateName { get; set; }

        public long LgaId { get; set; }
        public string LgaName { get; set; }

        // Geo data
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Estimated daily/market-day volume (kg)
        public decimal EstimatedCapacityKg { get; set; }

        // Relationships
        public List<MarketDayDto>? MarketDays { get; set; }
        public List<long>? CommodityIds { get; set; }
        public string Location { get; set; }
        public string? CreatedBy { get; set; }
     
    }
    public class MarketCommodityDto
    {
        public long MarketId { get; set; }

        public string CommodityName { get; set; }
        public long CommodityId { get; set; }
    }

    public class MarketDayDto
    {
        public long Id { get; set; }

        public long MarketId { get; set; }

        public string DayType { get; set; }
        public string? DayOfWeek { get; set; } // For weekly markets
        public int? CycleDay { get; set; }        // For 4-day cycles

        public bool IsActive { get; set; } = true;
    }
  
    public class CommodityDto
    {
          public long Id { get; set; }

        [Required]
        public string Name { get; set; } // Maize, Rice, Sorghum

        public long CommodityCategoryId { get; set; }
     
        public string StandardUnit { get; set; } // Base unit (KG)
        public string CommodityGrade { get; set; } // Base unit (KG)

        public ICollection<CommodityVarietyDto> Varieties { get; set; } = new List<CommodityVarietyDto>();
    }
   
    public class CommodityVarietyDto
    {
         public long Id { get; set; }

        public long CommodityId { get; set; }
       
        public string Name { get; set; } // Local Rice, Faro 44, Yellow Maize
    }

    public class CommodityCategoryDto
    {
         public long Id { get; set; }

        [Required]
        public string Name { get; set; } // Grains, Tubers, Legumes, Cash Crops

        public ICollection<CommodityDto> Commodities { get; set; } = new List<CommodityDto>();
    }
    public class CreateMarketDto
    {
        public string Name { get; set; }
        public string MarketType { get; set; }

        public long StateId { get; set; }
        public long LgaId { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public decimal EstimatedCapacityKg { get; set; }
        public string Location { get; set; }

        public List<MarketDayDto> MarketDays { get; set; } = new();
        public List<long> CommodityIds { get; set; } = new();
    }

}
