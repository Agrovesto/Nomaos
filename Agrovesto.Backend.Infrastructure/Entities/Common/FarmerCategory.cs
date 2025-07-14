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
        public string Name { get; set; }
        public long ValueChainTypeId { get; set; }

    }

    public class ValueChainType
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<FarmerValueChainItem> FarmerValueChainItem { get; set; }
        public long FarmerCategorysId { get; set; }
    }

    public class FarmerCategorys
    {

        public long Id { get; set; }
        public string FarmerCategory { get; set; }  // JSON field is "state"
        public List<ValueChainType> ValueChainType { get; set; }
    }

}
