using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
  


    public class WardModel
    {
        
        public long WardId { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public long LgaId { get; set; }

    }

    public class LGAModel
    {
        public long LgaId { get; set; }
        public string Name { get; set; }
        public List<WardModel> Wards { get; set; }
        public long StatesStateId { get; set; }
    }

    public class StateModel
    {
        
        public long StateId { get; set; }
        public string State { get; set; }  // JSON field is "state"
        public List<LGAModel> LGAs { get; set; }
    }


}
