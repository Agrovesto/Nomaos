using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class Countries
    {
        public string name { get; set; }
        public string iso2 { get; set; }
        public int id { get; set; }
    }
    public class StatesResponse
    {
        public List<States> States { get; set; }
    }
    public class States
    {
        public string name { get; set; }
        public string iso2 { get; set; }
        public int id { get; set; }
    }
    public class Cities
    {
        public string name { get; set; }
        public int id { get; set; }
    }
}
