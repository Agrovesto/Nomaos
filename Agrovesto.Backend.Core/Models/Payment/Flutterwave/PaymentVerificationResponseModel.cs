using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment.Flutterwave
{
   
    public class PaymentVerificationResponseModel
    {
        public string message { get; set; }
        public string status { get; set; }
        public WavePaymentVerificationDataModel data { get; set; }
    }
    public class WavePaymentVerificationDataModel
    {
        public long id { get; set; }
        public long amount { get; set; }
        public long app_fee { get; set; }
        public string status { get; set; }
    }
}
