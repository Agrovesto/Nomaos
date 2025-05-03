using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment.Flutterwave
{
    public class PaymentResponseModel
    {
        public string message { get; set; }
        public string status { get; set; }
        public string code { get; set; }
        public PaymentResponseDataModel data { get; set; }
    }
    public class PaymentResponseDataModel
    {
        public string link { get; set; }

        public decimal amount { get; set; }
        public string accountNumber { get; set; }
        public string bankName { get; set; }
        public string reference { get; set; }
        public string accountName { get; set; }
    }
}
