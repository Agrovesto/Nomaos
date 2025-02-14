using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class BusinessProject
    {
        public long ProjectId { get; set; }
        public long BusinessId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public bool IsProjectTimeBound { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string ProjectManager { get; set; }
        public IList<string> ProjectLocations { get; set; } //states
    }
}
