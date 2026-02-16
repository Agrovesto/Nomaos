using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest
{
    public class CollectionRequest
    {
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string ExternalReference { get; set; }
        public string AccountNumber { get; set; } // MSISDN
        public string Message { get; set; }
    }
    public class DisbursementRequest
    {
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string ExternalReference { get; set; }
        public string AccountNumber { get; set; }
        public string Message { get; set; }
    }
    public class CreateWalletRequest
    {
        public string UserId { get; set; }
        public string AccountNumber { get; set; }
    }
    public class PaymentResponse
    {
        public string Reference { get; set; }
        public string ProviderReference { get; set; }
        public string Status { get; set; } // Pending, Successful, Failed
    }

    public class PaymentStatusResponse
    {
        public string Reference { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class RequestToPayRequest
    {
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string ExternalId { get; set; } // UUID
        public string PartyId { get; set; } // MSISDN
        public string PayerMessage { get; set; }
        public string PayeeNote { get; set; }
    }

    public class PreApprovalRequest
    {
        public string PartyId { get; set; }
        public string PayerCurrency { get; set; }
        public string PayerMessage { get; set; }
        public string ValidityTime { get; set; }
    }

}
