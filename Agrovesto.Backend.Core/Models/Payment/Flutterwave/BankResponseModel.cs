using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment.Flutterwave
{
    public class BankResponseModel
    {
        public string status { get; set; }
        public string message { get; set; }
        public BankData[] data { get; set; }
    }

    public class BankData
    {
        public string code { get; set; }
        public string name { get; set; }
        public int id { get; set; }
    }
}
