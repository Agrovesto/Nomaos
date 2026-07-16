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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{
    public interface IEtransactWallet
    {
        
    }
    public class EtransactWallet : IGenericWalletService, IEtransactWallet
    {
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
        private class ProviderResponseEnvelope
        {
            public bool Success { get; set; }
            public string ResponseDescription { get; set; }
            public string Message { get; set; }
        }

        private class EtransactAccountCreationResponse
        {
            public bool Success { get; set; }
            public string ResponseDescription { get; set; }
            public string Message { get; set; }
            public string ResponseCode { get; set; }
            public EtransactWalletData Result { get; set; }
        }

        private class EtransactWalletData
        {
            public string WalletNumber { get; set; }
            public string AccountName { get; set; }
            public string MacCheckSum { get; set; }
            public string WalletTier { get; set; }
            public string WalletBalance { get; set; }
            // ... whatever Etransact's real success fields are called
        }
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
        private string BasicAuthHeader(string username, string password)
        {
            var authString = $"{username}:{password}";
            var authBytes = Encoding.UTF8.GetBytes(authString);
            return Convert.ToBase64String(authBytes);
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
            client.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.32.3");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }
        private async Task<MomoAuthResponse> GetProviderToken(
        ProviderParams @params, string hashKey, string clientIdentifier, string url)
        {
            var handler = new HttpClientHandler();

            using var client = new HttpClient(handler);

            ExtractDefaultHeader(hashKey, clientIdentifier, client);
            var content = new StringContent("", Encoding.UTF8, "application/json");

            var requesturl = $"{@params.BaseUrl}/{url}/token/";
            var response = await client.PostAsync($"{requesturl}", content);

            var json = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<MomoAuthResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        public async Task<WalletResponseService<AccountValidationResponse>> CreateWalletAsync(
    UserProfileResponse data,
    WalletProviderParameterModel[] parameters,
    string env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, env);

                var request = new
                {
                    emailAddress = !string.IsNullOrEmpty(data.Email) ? data.Email : "abc@maildrop.cc",
                    BVN = "12345678901", //data.BVN,
                    NIN = "12345678901", //data.NIN,
                    schemeId = @params.SchemeId,
                    image = !string.IsNullOrEmpty(data.ImageString) ? data.ImageString : "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
                    walletName = $"{data.FirstName}",
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
                    "/v1/new-customer/register",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();

                // Etransact can return HTTP 200 even when the request logically failed,
                // so the body's own "success" flag must be checked, not just the status code.
                var etransactResponse = JsonSerializer.Deserialize<EtransactAccountCreationResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!response.IsSuccessStatusCode || etransactResponse == null || !etransactResponse.Success)
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

        // Nested DTOs — matches Etransact's name-enquiry response shape
        private class EtransactNameEnquiryResponse
        {
            public EtransactNameEnquiryResult Result { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseDescription { get; set; }
            public string Message { get; set; }
            public bool Success { get; set; }
        }

        private class EtransactNameEnquiryResult
        {
            public string AccountName { get; set; }
            public string PhoneNumber { get; set; }
            public string EmailAddress { get; set; }
            public string EnquiryReference { get; set; }
        }
      
        public async Task<WalletResponseService<AccountValidationResponse>> AddExistingWallet(
      UserProfileResponse data,
      WalletProviderParameterModel[] parameters,
      string env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, env);

                var request = new 
                {
                    Bvn=data.BVN,
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

                var response = await client.PostAsync("/v1/new-customer/register", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var envelope = JsonSerializer.Deserialize<ProviderResponseEnvelope>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!response.IsSuccessStatusCode || envelope == null || !envelope.Success)
                {
                    return new WalletResponseService<AccountValidationResponse>
                    {
                        IsSuccess = false,
                        Message = envelope?.Message ?? 
                        envelope?.ResponseDescription ?? responseContent
                    };
                }

                var walletResponse =
                    JsonSerializer.Deserialize<AccountValidationResponse>(
                        responseContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

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

                var hashKey = GenerateHashKey(
                    @params.ClientSecret,
                    walletNumber);

                using var client = new HttpClient
                {
                    BaseAddress = new Uri(@params.BaseUrl)
                };

                ExtractDefaultHeader(
                    hashKey,
                    @params.ClientId,
                    client);

                var httpResponse = await client.GetAsync(
                    $"/v1/wallets/get-wallet-details/{walletNumber}");

                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                var etransactResponse = JsonSerializer.Deserialize<EtransactWalletDetailsResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return WalletResponseService<AccountValidationResponse>.Failed(
                        "Wallet account not found or not active",
                        404);
                }

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
        public async Task<WalletResponseService<AccountValidationResponse>> BalanceEnquiry(
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

        // Nested DTOs — matches Etransact's balance-enquiry response shape
        private class EtransactBalanceEnquiryResponse
        {
            public EtransactBalanceEnquiryResult Result { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseDescription { get; set; }
            public string Message { get; set; }
            public bool Success { get; set; }
        }

        private class EtransactBalanceEnquiryResult
        {
            public string AccountName { get; set; }
            public decimal Balance { get; set; }
            public decimal LedgerBalance { get; set; }
        }
        // Nested DTOs — matches Etransact's get-wallet-details response shape
        private class EtransactWalletDetailsResponse
        {
            public EtransactWalletDetailsResult Result { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseDescription { get; set; }
            public string Message { get; set; }
            public bool Success { get; set; }
        }

        private class EtransactWalletDetailsResult
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

        private class EtransactCustomer
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

        private class EtransactWalletTier
        {
            public string Id { get; set; }
            public string LimitConfigurationId { get; set; }
            public string TierName { get; set; }
            public int TierLevel { get; set; }
            public string Name { get; set; }
            public string WalletSchemeId { get; set; }
        }
        public Task<WalletResponseService<PaymentStatusResponse>> GetTransactionStatusAsync(string reference, WalletProviderParameterModel[] parameters, string Env)
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

        // Nested DTO — matches Etransact's debit-wallet response shape (no `result` payload)
        private class EtransactDebitWalletResponse
        {
            public string ResponseCode { get; set; }
            public string ResponseDescription { get; set; }
            public string Message { get; set; }
            public bool Success { get; set; }
        }
    }
    
}
