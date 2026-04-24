using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    internal class SystemConfigs
    {
    }
    public class Module
    {
        public long Id { get; set; }
        public string Name { get; set; } // Sales, Trading, Inventory
        public string Code { get; set; } // SALES, TRD, INV
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string? CreatedByUserId { get; set; }
        public string? BusinessCode { get; set; }
        public ICollection<LocationModule> LocationModules { get; set; }
     = new List<LocationModule>();
    }
    public class StorageCategory
    {
        public long Id { get; set; }
        public string Name { get; set; } // Grains, Livestock, Fertilizer
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now; 
        public string? CreatedByUserId { get; set; }
        public string? BusinessCode { get; set; }
        public ICollection<LocationStorageCategory> LocationStorageCategories { get; set; } = new List<LocationStorageCategory>();
    }
    public class TemperatureControlType
    {
        public long Id { get; set; }
        public string Name { get; set; } // Cold Storage, Refrigerated, Ambient
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string? CreatedByUserId { get; set; }
        public string? BusinessCode { get; set; }
    }
 }
