using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    internal class NigerianStatesDetail
    {
    }

    public class Ward
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long WardId { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public long LgaId { get; set; }
        public LGA LGA { get; set; }

    }

    public class LGA
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long LgaId { get; set; }
        public string Name { get; set; }
        public List<Ward> Wards { get; set; }
        public long StatesStateId { get; set; }
        public States States { get; set; }
    }

    public class States
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long StateId { get; set; }
        public string State { get; set; }  // JSON field is "state"
        public List<LGA> LGAs { get; set; }
    }


}
