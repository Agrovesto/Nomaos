using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class FarmerImagesModel
    {
        public long Id { get; set; }
        public string ImageName { get; set; }
        public string FarmerUserId { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string ImageKey { get; set; }
    }
}
