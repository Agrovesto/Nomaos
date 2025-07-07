using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment.Flutterwave
{
    public class FlutterwaveWebhookResponseBody
    {
        public string Event { get; set; }
        public EventData Data { get; set; }
    
    }

    public class EventData
    {
        public int Id { get; set; }
        public string tx_ref { get; set; }
        public string flw_ref { get; set; }
        public string device_fingerprint { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public decimal charged_amount { get; set; }
        public double app_fee { get; set; }
        public int merchant_fee { get; set; }
        public string processor_response { get; set; }
        public string auth_model { get; set; }
        public string ip { get; set; }
        public string narration { get; set; }
        public string status {
            get;
        set; }
        public string payment_type { get; set; }
        public DateTimeOffset created_at {
            get;
        set; }
        public int account_id { get; set; }
        public Customer customer { get; set; }
    }
}
