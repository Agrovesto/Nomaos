using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class FarmerValueChainItemModel
    {

        public long Id { get; set; }
        public string Name { get; set; }
        public long ValueChainTypeId { get; set; }

    }

    public class ValueChainTypeModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<FarmerValueChainItemModel> FarmerValueChainItem { get; set; }
        public long FarmerCategoryId { get; set; }
    }

    public class FarmerCategoryModel
    {

        public long Id { get; set; }
        public string FarmerCategory { get; set; }  // JSON field is "state"
        public List<ValueChainTypeModel> ValueChainType { get; set; }
    }

}
