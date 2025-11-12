using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class FarmerValueChainItem
    {
        public long Id { get; set; }
        public long FarmerId { get; set; }
        public string ValueChainType { get; set; } // Crop, Animal, Aquaculture
        public string ItemName { get; set; }

        public Farmer Farmer { get; set; }
    }



    public enum ValueChainType
    {
        Crop,
        Livestock,
        Aquaculture
    }


    public enum FarmerCategory
    {
        Production,
        Processing,
        Both
    }


}
