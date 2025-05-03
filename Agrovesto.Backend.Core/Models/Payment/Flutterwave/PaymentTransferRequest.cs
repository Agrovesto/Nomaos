using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment.Flutterwave
{
    public class PaymentTransferRequest
    {

        public string account_bank { get; set; }
        public string account_number { get; set; }
        public int amount { get; set; }
        public string narration { get; set; }
        public string currency { get; set; } = "NGN";
        public string reference { get; set; }
        public string callback_url { get; set; }
        public string debit_currency { get; set; } = "NGN";
    //    public string beneficiary_name { get; set; }
    }
}
