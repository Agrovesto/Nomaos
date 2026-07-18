using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{
    public class ETransactModels
    {
        private class ProviderResponseEnvelope
        {
            public bool Success { get; set; }
            public string ResponseDescription { get; set; }
            public string Message { get; set; }
        }



    }
    public interface IEtransactWallet
    {

    }
    public class EtransactWallet : IGenericWalletService, IEtransactWallet
    {
        private const string defaultImageBase64 = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyMDAiIGhlaWdodD0iMjAwIiB2aWV3Qm94PSIwIDAgMjAwIDIwMCI+CiAgPGNpcmNsZSBjeD0iMTAwIiBjeT0iMTAwIiByPSI5MCIgZmlsbD0iI2YyYzI5ZSIvPgogIDxjaXJjbGUgY3g9IjcwIiBjeT0iOTAiIHI9IjEwIiBmaWxsPSIjMzMzIi8+CiAgPGNpcmNsZSBjeD0iMTMwIiBjeT0iOTAiIHI9IjEwIiBmaWxsPSIjMzMzIi8+CiAgPHBhdGggZD0iTTcwIDEzMCBRMTAwIDE1NSAxMzAgMTMwIiBzdHJva2U9IiMzMzMiIHN0cm9rZS13aWR0aD0iNSIgZmlsbD0ibm9uZSIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIi8+CiAgPHBhdGggZD0iTTQwIDcwIFE2MCA0MCAxMDAgNDAgUTE0MCA0MCAxNjAgNzAiIHN0cm9rZT0iIzVhMzgyNSIgc3Ryb2tlLXdpZHRoPSIxNSIgZmlsbD0ibm9uZSIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIi8+Cjwvc3ZnPgo=";
        private readonly IHttpService _httpService;
        private readonly ILogger<EtransactWallet> _log;
        private readonly IConfiguration _configuration;


        public EtransactWallet(ILogger<EtransactWallet> log, IHttpService httpService, IConfiguration configuration)
        {

            _httpService = httpService;
            _configuration = configuration;
            _log = log;
        }
        public string ProviderKey => "Etransact";
        public bool RequiresPreDebit => true;
        public async Task<ProviderParams> RetrieveParams(WalletProviderParameterModel[] parameters, string Env)
        {
            var response = new ProviderParams();

            var _params = parameters.ToDictionary(x => x.Key);

            response.BaseUrl = _params["BaseUrl"].Value;
            response.ClientSecret = _params["ClientSecret"].Value;
            response.ClientId = _params["ClientId"].Value;
            response.SchemeId = _params["SchemeId"].Value;
            response.WalletShemeTierId = _params["WalletShemeTierId"].Value;


            return response;

        }
        public async Task<WalletResponseService<AccountValidationResponse>> CreateWalletAsync(
            UserProfileResponse data,
            WalletProviderParameterModel[] parameters,
            string env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, env);

                System.Diagnostics.Debug.WriteLine(
                    $"[WalletCreate] env={env} clientId={@params.ClientId} schemeId={@params.SchemeId} baseUrl={@params.BaseUrl}");

                var request = new
                {
                    emailAddress = !string.IsNullOrEmpty(data.Email) ? data.Email : "abc@maildrop.cc",
                    BVN = data.BVN,
                    NIN = data.NIN,
                    schemeId = @params.SchemeId,
                    image = !string.IsNullOrEmpty(data.ImageString)
                        ? data.ImageString
                        : defaultImageBase64,
                    walletName = $"{data.FirstName}",
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var payloadJson = JsonSerializer.Serialize(request, jsonOptions);

                var hashKey = GenerateHashKey(@params.ClientSecret, payloadJson);

                System.Diagnostics.Debug.WriteLine($"[WalletCreate] payloadJson={payloadJson}");
                System.Diagnostics.Debug.WriteLine($"[WalletCreate] hashKey={hashKey}");

                using var client = new HttpClient
                {
                    BaseAddress = new Uri(@params.BaseUrl)
                };

                ExtractDefaultHeader(hashKey, @params.ClientId, client);

                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/v1/new-customer/register", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"[WalletCreate] status={(int)response.StatusCode} body={responseContent}");

                var etransactResponse = JsonSerializer.Deserialize<EtransactAccountCreationResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!response.IsSuccessStatusCode
                    && etransactResponse == null 
                    )
                {
                    return new WalletResponseService<AccountValidationResponse>
                    {
                        IsSuccess = false,
                        Message = etransactResponse?.Message
                                  ?? etransactResponse?.ResponseDescription
                                  ?? responseContent
                    };
                }

                var walletResponse = new AccountValidationResponse
                {
                    WalletNumber = etransactResponse.Result?.WalletNumber,
                    AccountName = etransactResponse.Result?.AccountName
                };

                return new WalletResponseService<AccountValidationResponse>
                {
                    IsSuccess = true,
                    Data = walletResponse,
                    Message = "Wallet created successfully."
                };
            }
            catch (Exception ex)
            {
                return new WalletResponseService<AccountValidationResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<WalletResponseService<AccountValidationResponse>> GetWalletDetailsAsync(
            string walletNumber,
            WalletProviderParameterModel[] parameters,
            string env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, env);

                var hashKey = GenerateHashKey(@params.ClientSecret, string.Empty);

                System.Diagnostics.Debug.WriteLine(
                    $"[WalletDetails] walletNumber={walletNumber} hashKey={hashKey}");

                using var client = new HttpClient
                {
                    BaseAddress = new Uri(@params.BaseUrl)
                };

                ExtractDefaultHeader(hashKey, @params.ClientId, client);

                var httpResponse = await client.GetAsync($"/v1/wallets/get-wallet-details/{walletNumber}");

                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"[WalletDetails] status={(int)httpResponse.StatusCode} body={responseContent}");

                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return WalletResponseService<AccountValidationResponse>.Failed(
                        "Wallet account not found or not active",
                        404);
                }

                var etransactResponse = JsonSerializer.Deserialize<EtransactWalletDetailsResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!httpResponse.IsSuccessStatusCode || etransactResponse == null || !etransactResponse.Success)
                {
                    var errorMessage = etransactResponse?.Message
                        ?? etransactResponse?.ResponseDescription
                        ?? responseContent;

                    return WalletResponseService<AccountValidationResponse>.Failed(
                        $"Provider error: {httpResponse.StatusCode} - {errorMessage}",
                        (int)httpResponse.StatusCode);
                }

                var result = etransactResponse.Result;

                var walletDetails = new AccountValidationResponse
                {
                    WalletNumber = result?.WalletNumber,
                    AccountName = result?.Customer != null
                        ? $"{result.Customer.FirstName} {result.Customer.LastName}".Trim()
                        : null
                };

                return WalletResponseService<AccountValidationResponse>.Success(
                    walletDetails,
                    etransactResponse.ResponseDescription ?? "Wallet details retrieved successfully");
            }
            catch (HttpRequestException ex)
            {
                return WalletResponseService<AccountValidationResponse>.Failed(
                    "Unable to reach Etransact service: " + ex.Message,
                    503);
            }
            catch (Exception ex)
            {
                return WalletResponseService<AccountValidationResponse>.Failed(
                    ex.Message,
                    500);
            }
        }
        public async Task<WalletResponseService<AccountValidationResponse>> NameEnquiry(
 UserProfileResponse data,
 WalletProviderParameterModel[] parameters,
 string env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, env);

                var request = new
                {
                    walletNumber = data.WalletNumber,
                };

                var payloadJson = JsonSerializer.Serialize(request);

                var hashKey = GenerateHashKey(
                    @params.ClientSecret,
                    payloadJson);

                using var client = new HttpClient
                {
                    BaseAddress = new Uri(@params.BaseUrl)
                };

                ExtractDefaultHeader(
                    hashKey,
                    @params.ClientId,
                    client);

                var content = new StringContent(
                    payloadJson,
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    "/v1/wallets/name-enquiry",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();

                var etransactResponse = JsonSerializer.Deserialize<EtransactNameEnquiryResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!response.IsSuccessStatusCode || etransactResponse == null || !etransactResponse.Success)
                {
                    var errorMessage = etransactResponse?.Message
                        ?? etransactResponse?.ResponseDescription
                        ?? responseContent;

                    return WalletResponseService<AccountValidationResponse>.Failed(
                        $"Provider error: {response.StatusCode} - {errorMessage}",
                        (int)response.StatusCode);
                }

                var walletResponse = new AccountValidationResponse
                {
                    WalletNumber = data.WalletNumber,
                    AccountName = etransactResponse.Result?.AccountName
                };

                return WalletResponseService<AccountValidationResponse>.Success(
                    walletResponse,
                    etransactResponse.ResponseDescription ?? "Name enquiry successful");
            }
            catch (HttpRequestException ex)
            {
                return WalletResponseService<AccountValidationResponse>.Failed(
                    "Unable to reach Etransact service: " + ex.Message,
                    503);
            }
            catch (Exception ex)
            {
                return WalletResponseService<AccountValidationResponse>.Failed(
                    ex.Message,
                    500);
            }
        }


      //  public async Task<WalletResponseService<AccountValidationResponse>> AddExistingWallet(
      //UserProfileResponse data,
      //WalletProviderParameterModel[] parameters,
      //string env)
      //  {
      //      try
      //      {
      //          var @params = await RetrieveParams(parameters, env);

      //          var request = new
      //          {
      //              Bvn = data.BVN,
      //              walletNumber = data.WalletNumber,
      //          };

      //          var payloadJson = JsonSerializer.Serialize(request);

      //          var hashKey = GenerateHashKey(
      //              @params.ClientSecret,
      //              payloadJson);

      //          using var client = new HttpClient
      //          {
      //              BaseAddress = new Uri(@params.BaseUrl)
      //          };

      //          ExtractDefaultHeader(
      //              hashKey,
      //              @params.ClientId,
      //              client);

      //          var content = new StringContent(
      //              payloadJson,
      //              Encoding.UTF8,
      //              "application/json");

      //          var response = await client.PostAsync("/v1/new-customer/register", content);
      //          var responseContent = await response.Content.ReadAsStringAsync();

      //          var envelope = JsonSerializer.Deserialize<ProviderResponseEnvelope>(
      //              responseContent,
      //              new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

      //          if (!response.IsSuccessStatusCode || envelope == null || !envelope.Success)
      //          {
      //              return new WalletResponseService<AccountValidationResponse>
      //              {
      //                  IsSuccess = false,
      //                  Message = envelope?.Message ??
      //                  envelope?.ResponseDescription ?? responseContent
      //              };
      //          }

      //          var walletResponse =
      //              JsonSerializer.Deserialize<AccountValidationResponse>(
      //                  responseContent,
      //                  new JsonSerializerOptions
      //                  {
      //                      PropertyNameCaseInsensitive = true
      //                  });

      //          return new WalletResponseService<AccountValidationResponse>
      //          {
      //              IsSuccess = true,
      //              Data = walletResponse,
      //              Message = "Wallet created successfully."
      //          };
      //      }
      //      catch (Exception ex)
      //      {
      //          return new WalletResponseService<AccountValidationResponse>
      //          {
      //              IsSuccess = false,
      //              Message = ex.Message
      //          };
      //      }
      //  }

        public async Task<WalletResponseService<AccountValidationResponse>> BalanceEnquiry(
    UserProfileResponse data,
    WalletProviderParameterModel[] parameters,
    string env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, env);
                System.Diagnostics.Debug.WriteLine(
                    $"[BalanceEnquiry] env={env} clientId={@params.ClientId} schemeId={@params.SchemeId} baseUrl={@params.BaseUrl}");
                var request = new
                {
                    walletNumber = data.WalletNumber,
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var payloadJson = JsonSerializer.Serialize(request, jsonOptions);

                var hashKey = GenerateHashKey(@params.ClientSecret, payloadJson);

                System.Diagnostics.Debug.WriteLine($"[BalanceEnquiry] payloadJson={payloadJson}");
                System.Diagnostics.Debug.WriteLine($"[BalanceEnquiry] hashKey={hashKey}");

                using var client = new HttpClient
                {
                    BaseAddress = new Uri(@params.BaseUrl)
                };

                ExtractDefaultHeader(hashKey, @params.ClientId, client);

                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    "/v1/wallets/balance-enquiry",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();

                var etransactResponse = JsonSerializer.Deserialize<EtransactBalanceEnquiryResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!response.IsSuccessStatusCode || etransactResponse == null || !etransactResponse.Success)
                {
                    var errorMessage = etransactResponse?.Message
                        ?? etransactResponse?.ResponseDescription
                        ?? responseContent;

                    return WalletResponseService<AccountValidationResponse>.Failed(
                        $"Provider error: {response.StatusCode} - {errorMessage}",
                        (int)response.StatusCode);
                }

                var walletResponse = new AccountValidationResponse
                {
                    WalletNumber = data.WalletNumber,
                    AccountName = etransactResponse.Result?.AccountName
                };

                return WalletResponseService<AccountValidationResponse>.Success(
                    walletResponse,
                    etransactResponse.ResponseDescription ?? "Balance enquiry successful");
            }
            catch (HttpRequestException ex)
            {
                return WalletResponseService<AccountValidationResponse>.Failed(
                    "Unable to reach Etransact service: " + ex.Message,
                    503);
            }
            catch (Exception ex)
            {
                return WalletResponseService<AccountValidationResponse>.Failed(
                    ex.Message,
                    500);
            }
        }


        public async Task<WalletResponseService<PaymentStatusResponse>> GetTransactionStatusAsync(string reference, WalletProviderParameterModel[] parameters, string Env)
        {
            throw new NotImplementedException();
        }

        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTopUpAsync(WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string Env)
        {
            throw new NotImplementedException();
        }

        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTransferAsync(TransferRequest request, WalletProviderParameterModel[] parameters, string Env)
        {
            throw new NotImplementedException();
        }

        public async Task<WalletResponseService<PaymentResponse>> DebitWalletAsync(
       WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string Env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, Env);

                var body = new
                {
                    walletNumber = request.WalletNumber,
                    //walletName = request.WalletName,
                    transactionReference = request.TransactionReference,
                    amount = request.Amount.ToString("0.00"),
                    //fee = request.Fee,
                    //stampDutyFee = request.StampDutyFee,
                    narration = request.Message
                };

                var payloadJson = JsonSerializer.Serialize(body);
                var hashKey = GenerateHashKey(
                             @params.ClientSecret,
                             payloadJson
                             //,
                             //request.CustomerMac
                             );

                using var client = new HttpClient
                {
                    BaseAddress = new Uri(@params.BaseUrl)
                };

                ExtractDefaultHeader(
                    hashKey,
                    @params.ClientId,
                    client);

                var content = new StringContent(
                    payloadJson,
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    "/v1/wallets/debit-wallet",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();

                var etransactResponse = JsonSerializer.Deserialize<EtransactDebitWalletResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!response.IsSuccessStatusCode || etransactResponse == null || !etransactResponse.Success)
                {
                    var errorMessage = etransactResponse?.Message
                        ?? etransactResponse?.ResponseDescription
                        ?? responseContent;

                    return WalletResponseService<PaymentResponse>.Failed(
                        $"Request failed with status {response.StatusCode}: {errorMessage}",
                        (int)response.StatusCode);
                }

                var paymentResponse = new PaymentResponse
                {
                    Status = etransactResponse.ResponseDescription,
                    Reference = request.TransactionReference,
                    ProviderReference = request.TransactionReference
                };

                return WalletResponseService<PaymentResponse>.Success(
                    paymentResponse,
                    etransactResponse.ResponseDescription ?? "Debit successful");
            }
            catch (HttpRequestException ex)
            {
                return WalletResponseService<PaymentResponse>.Failed(
                    "Unable to reach Etransact service: " + ex.Message,
                    503);
            }
            catch (Exception ex)
            {
                return WalletResponseService<PaymentResponse>.Failed(
                    ex.Message,
                    500);
            }
        }

        private string GenerateHashKey(
            string clientSecret,
            string payloadJson = "",
            string customerMac = null)
        {
            payloadJson ??= string.Empty;

            var stringToHash = string.IsNullOrWhiteSpace(customerMac)
                ? $"{clientSecret}{payloadJson}"
                : $"{customerMac}{clientSecret}{payloadJson}";

            return HashingHelper.ComputeSha256Hash(stringToHash);
        }

        private static void ExtractDefaultHeader(string hashKey, string clientIdentifier, HttpClient client)
        {
            client.DefaultRequestHeaders.Clear();

            client.DefaultRequestHeaders.Add("x-Client-Id", clientIdentifier);
            client.DefaultRequestHeaders.Add("x-hash-key", hashKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }




    
    }

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
