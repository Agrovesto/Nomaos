using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment.Flutterwave
{
    public class PaymentRequestModel
    {
        public string tx_ref { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; } = "NGN";
        public string redirect_url { get; set; }
        public Customization customizations { get; set; }
        public Customer customer { get; set; }
    }
    public class Customization
    {
        public string title { get; set; }
        public string description { get; set; }
        public string logo { get; set; } = "https://cdn.filestackcontent.com/II7A4aM8TLyXzy9UUNtw";
    }
    public class Customer
    {
        public string email { get; set; }
        public string phonenumber { get; set; }
        public string name { get; set; }
    }
}
