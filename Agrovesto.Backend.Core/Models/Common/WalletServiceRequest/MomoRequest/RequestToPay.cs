using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest
{
    public class ReconciliationAlertRequest
    {
        public string senderEmail { get; set; }
        public string RecipientEmail { get; set; }
        public string Subject { get; set; }
        public string bodyHtm { get; set; }
        public string bodyText { get; set; }
        public string DebitReference { get; set; }
        public string CreditReference { get; set; }
        public string Reason { get; set; }
        public DateTimeOffset FlaggedAt { get; set; }
    }
    public class CollectionRequest
    {
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string Reference { get; set; }
        public string AccountNumber { get; set; } // MSISDN
        public string Message { get; set; }
    }
    public class TransferRequest
    {
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string Reference { get; set; }
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
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
    public class MomoPreApprovalResponse
    {
        [JsonPropertyName("preApprovalDetails")]
        public List<PreApprovalResponseData> PreApprovalDetails { get; set; }

    }
    public class MomoPaymentStatusResponse
    {
        [JsonPropertyName("financialTransactionId")]
        public string FinancialTransactionId { get; set; }

        [JsonPropertyName("externalId")]
        public string ExternalId { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("payer")]
        public Payer Payer { get; set; }

        [JsonPropertyName("payerMessage")]
        public string PayerMessage { get; set; }

        [JsonPropertyName("payeeNote")]
        public string PayeeNote { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class Payer
    {
        [JsonPropertyName("partyIdType")]
        public string PartyIdType { get; set; }

        [JsonPropertyName("partyId")]
        public string PartyId { get; set; }
    }
    public class PreApprovalResponseData
    {
        [JsonPropertyName("preApprovalId")]
        public string PreApprovalId { get; set; }

        [JsonPropertyName("toFri")]
        public string ToFri { get; set; }

        [JsonPropertyName("fromFri")]
        public string FromFri { get; set; }

        [JsonPropertyName("fromCurrency")]
        public string FromCurrency { get; set; }

        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("approvedTime")]
        public DateTime ApprovedTime { get; set; }

        [JsonPropertyName("expiryTime")]
        public DateTime ExpiryTime { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("lastUsedDate")]
        public DateTime LastUsedDate { get; set; }

        [JsonPropertyName("offer")]
        public string Offer { get; set; }

        [JsonPropertyName("externalId")]
        public string ExternalId { get; set; }
    }
    public class MomoPreApprovalRequest
    {
        public string MSISDN { get; set; }
        public string Currency { get; set; }
        public string Message { get; set; }
        public string Reference { get; set; }
    }
    public class MomoPaymentStatusDto
    {
        [JsonPropertyName("financialTransactionId")]
        public string FinancialTransactionId { get; set; }

        [JsonPropertyName("referenceId")]
        public string ReferenceId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
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
    public class MomoAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
    public class ProviderParams
    {
        public string BaseUrl { get; set; }
        public string SubscriptionKey { get; set; }
        public string ProviderKey { get; set; }
        public string UserName { get; set; }
        public string SubscriptionApiKey { get; set; }
        public string DisbursementApiKey { get; set; }
        public string TargetEnvironment { get; set; }
    }
}
