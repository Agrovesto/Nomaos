using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment
{

    public class FlexibleStringConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetDecimal(out var dec) ? dec.ToString() : reader.GetDouble().ToString(),
                JsonTokenType.Null => null,
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                _ => throw new JsonException($"Unexpected token {reader.TokenType} for string-like field.")
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }

    public class EtransactAccountCreationResponse
    {
        // Real response uses "status" for the success flag, not "success" -
        // PropertyNameCaseInsensitive only fixes casing, not different names.
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("responseDescription")]
        public string ResponseDescription { get; set; }

        public string Message { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("result")]
        public EtransactWalletData Result { get; set; }
    }

    public class EtransactWalletData
    {
        [System.Text.Json.Serialization.JsonPropertyName("walletNumber")]
        public string WalletNumber { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("walletName")]
        public string AccountName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("macChecksum")]
        public string MacCheckSum { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("walletTier")]
        public string WalletTier { get; set; }

        // Tolerant of the API returning this as a JSON number instead of a string.
        [System.Text.Json.Serialization.JsonPropertyName("walletBalance")]
        [System.Text.Json.Serialization.JsonConverter(typeof(FlexibleStringConverter))]
        public string WalletBalance { get; set; }
    }
    public class EtransactDebitWalletResponse
    {
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }
    public class EtransactBalanceEnquiryResponse
    {
        public EtransactBalanceEnquiryResult Result { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class NameEnquiryResult
    {
        public string AccountName { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        public string EnquiryReference { get; set; }
    }

    public class WalletBalanceResult
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public decimal LedgerBalance { get; set; }
    }


    public class NameEnquiryEtransactResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("result")]
        public NameEnquiryData Result { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("responseDescription")]
        public string ResponseDescription { get; set; }

        public string Message { get; set; }

        // This endpoint uses "success", unlike the create-wallet endpoint which
        // uses "status" - the two are NOT interchangeable across endpoints.
        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    public class BalanceEnquiryEtransactResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("result")]
        public BalanceEnquiryData Result { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("responseDescription")]
        public string ResponseDescription { get; set; }

        public string Message { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    public class BalanceEnquiryData
    {
        [System.Text.Json.Serialization.JsonPropertyName("accountName")]
        public string AccountName { get; set; }

        // Numbers here (145430.0), unlike walletBalance in the create-wallet
        // response which is a string ("0.00") - each endpoint's numeric fields
        // need to be checked individually rather than assumed consistent.
        [System.Text.Json.Serialization.JsonPropertyName("balance")]
        public decimal? Balance { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("ledgerBalance")]
        public decimal? LedgerBalance { get; set; }
    }

    public class TransactionOperationEtransactResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("responseDescription")]
        public string ResponseDescription { get; set; }

        public string Message { get; set; }
    }

    /// <summary>
    /// Friendly descriptions for Etransact's documented HTTP status codes, used
    /// to produce clearer error messages than a bare status code.
    /// </summary>
    public static class HttpStatusCodeHelper
    {
        public static string Describe(System.Net.HttpStatusCode statusCode) => (int)statusCode switch
        {
            200 => "Success - request processed",
            400 => "Bad Request - invalid parameters",
            401 => "Unauthorized - invalid authentication (check x-Client-Id / x-hash-key)",
            403 => "Forbidden - insufficient permissions",
            404 => "Not Found - resource doesn't exist",
            409 => "Conflict - duplicate request",
            422 => "Unprocessable - business rule violation",
            429 => "Too Many Requests - rate limit exceeded",
            500 => "Server Error - internal server issue at provider",
            502 => "Bad Gateway - upstream service error",
            503 => "Service Unavailable - maintenance/downtime",
            _ => $"Unexpected status code {(int)statusCode}"
        };
    }
    public class EtransactBalanceEnquiryResult
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public decimal LedgerBalance { get; set; }
    }
    // Nested DTOs — matches Etransact's get-wallet-details response shape
    public class EtransactWalletDetailsResponse
    {
        public EtransactWalletDetailsResult Result { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class EtransactWalletDetailsResult
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string WalletNumber { get; set; }
        public decimal Balance { get; set; }
        public bool IsRisk { get; set; }
        public bool IsPND { get; set; }
        public string Status { get; set; }
        public DateTime? DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public EtransactCustomer Customer { get; set; }
        public EtransactWalletTier WalletTier { get; set; }
    }

    public class EtransactCustomer
    {
        public string Id { get; set; }
        public string Bvn { get; set; }
        public string Nin { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string OtherNames { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        public string Gender { get; set; }
        public string Status { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string MacChecksum { get; set; }
    }

    public class EtransactWalletTier
    {
        public string Id { get; set; }
        public string LimitConfigurationId { get; set; }
        public string TierName { get; set; }
        public int TierLevel { get; set; }
        public string Name { get; set; }
        public string WalletSchemeId { get; set; }
    }
    public class EtransactNameEnquiryResponse
    {
        public EtransactNameEnquiryResult Result { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class EtransactNameEnquiryResult
    {
        public string AccountName { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        public string EnquiryReference { get; set; }
    }

}
