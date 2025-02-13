using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class FarmDetails
    {
        public long Id { get; set; }
        public long FarmerId { get; set; }
        public string Coordinate1 { get; set; }
        public string Coordinate2 { get; set; }
        public string Coordinate3 { get; set; }
        public string Coordinate4 { get; set; }
        public string CoordinateConversionInHectare { get; set; }
        public string FarmLocation { get; set; }
        public string FarmState { get; set; }
        public string FarmLga { get; set; }
    }
}
