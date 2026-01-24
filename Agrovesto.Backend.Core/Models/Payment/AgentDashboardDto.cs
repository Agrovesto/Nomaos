using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment
{
    public class AgentDashboardDto
    {
        // Top-level summary
        public long TotalCooperatives { get; set; }
        public long TotalFarmers { get; set; }
        public double TotalFarmSizeHectares { get; set; }
        public long TotalFarms { get; set; }

        // Operational overview
        public long MappedFarms { get; set; }
        public long UnmappedFarms { get; set; }

        // Farmers overview
        public long FarmersLinkedToCooperatives { get; set; }
        public long FarmersNotLinkedToCooperatives { get; set; }
    }
    public class FarmMappersDto
    {
        public string Id { get; set; }
        public string FarmerId { get; set; }
        public string AgentId { get; set; }
        public string CooperativeId { get; set; }
        public double SizeHectares { get; set; }
        public bool IsMapped { get; set; }
    }

}
