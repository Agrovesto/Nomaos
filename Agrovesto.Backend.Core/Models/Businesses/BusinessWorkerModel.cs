using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class BusinessWorkerModel
    {
        public long BusinessWorkerId { get; set; }
        public string UserId { get; set; }

        public long BusinessId { get; set; }
    }
}
