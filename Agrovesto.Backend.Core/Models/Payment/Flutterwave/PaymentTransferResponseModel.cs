using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment.Flutterwave
{
    
    public class PaymentTransferResponseModel
    {
        public string status { get; set; }
        public string message { get; set; }
        public PaymentTransferResponseDataModel data { get; set; }
    }

    public class PaymentTransferResponseDataModel
    {
        public long id { get; set; }
        public string account_number { get; set; }
        public string account_name { get; set; }
        public string bank_code { get; set; }
        public string full_name { get; set; }
        public string created_at { get; set; }
        public string currency { get; set; }
        public string debit_currency { get; set; }
        public decimal amount { get; set; }
        public decimal fee { get; set; }
        public string status { get; set; }
        public string reference { get; set; }
        public string meta { get; set; }
        public string narration { get; set; }
        public string complete_message { get; set; }
        public string bank_name { get; set; }

    }
}

