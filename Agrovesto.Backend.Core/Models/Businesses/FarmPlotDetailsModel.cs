using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
  
    public class FarmPlotDetailsModel
    {
        public long Id { get; set; }
        public long FarmerId { get; set; }
        public string? FarmerName { get; set; }
        public double Latitude1 { get; set; }
        public double Longitude1 { get; set; }
        public double Latitude2 { get; set; }
        public double Longitude2 { get; set; }
        public double Latitude3 { get; set; }
        public double Longitude3 { get; set; }
        public double Latitude4 { get; set; }
        public double Longitude4 { get; set; }
        public string FarmLocation { get; set; }
        public string FarmState { get; set; }
        public string FarmLga { get; set; }
        public string CoordinateTakenBy {  get; set; }
        public string CoordinateTakenByUserId { get; set; }
        public double AreaInDegress { get; set; }
        public double AreaInSqMetre { get; set; }
        public double AreaInHectare { get; set; }

        public string? BelongToBusinessCode { get; set; }
    }
}
