using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class TermiiResponse
    {
        public string message_id { get; set; }
        public string message { get; set; }
        public double balance { get; set; }
        public string user { get; set; }
    }
    public class TermiiSmsRequest
    {
        public string to { get; set; }
        public string from { get; set; }
        public string sms { get; set; }
        public string type { get; set; }
        public string api_key { get; set; }
        public string channel { get; set; }
        public TermiiMedia media { get; set; }
    }

    public class TermiiMedia
    {
        public string url { get; set; }
        public string caption { get; set; }
    }
    public class SMSResponseDetails
    {
        public string was_successful { get; set; }
        public string message { get; set; }
        public long cost { get; set; }
        public decimal balance { get; set; }
        public long required { get; set; }
        public long total_receivers { get; set; }
    }
    public class SMSResponseData
    {
        public string data { get; set; }
        public string message { get; set; }
        public SMSResponseDetails details { get; set; }
    }
    public class SMSResponseModel
    {
        public bool status { get; set; }
        public string message { get; set; }
        public SMSResponseData data { get; set; }

    }
    public class WhisperSMSModel
    {
        public List<string> contacts { get; set; }
        public string sender_id { get; set; }
        public string message { get; set; }
        public string send_date { get; set; }
        public bool priority_route { get; set; } = true;
        public string campaign_name { get; set; }
    }

    public class AfricaSMSResponse
    {
        public string Date { get; set; }
        public string From { get; set; }
        public string Id { get; set; }
        public string LinkId { get; set; }
        public string Text { get; set; }
        public string NetworkCode { get; set; }
        public string To { get; set; }

    }
}
