using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Core.Models.Payment;
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
        Task<WalletResponseService<PaymentResponse>> ReverseTransactionAsync(string transactionReference, WalletProviderParameterModel[] parameters, string env);
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


        public async Task<ProviderParams> RetrieveParams(WalletProviderParameterModel[] parameters, string Env)
        {
            var response = new ProviderParams();

            var _params = parameters.ToDictionary(x => x.Key);

            response.BaseUrl = _params["BaseUrl"].Value;
            response.ClientSecret = _params["ClientSecret"].Value;
            response.ClientId = _params["ClientId"].Value;
            response.SchemeId = _params["SchemeId"].Value;
            response.WalletShemeTierId = _params["WalletShemeTierId"].Value;
            response.WalletFee = _params["Fee"].Value;
            response.WalletStampDuty = _params["StampDuty"].Value;


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
                    AccountName = etransactResponse.Result?.AccountName,
                    macChecksum= etransactResponse.Result.MacCheckSum
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
        public async Task<WalletResponseService<NameEnquiryResult>> NameEnquiryAsync(
         string walletNumber,
         WalletProviderParameterModel[] parameters,
         string env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, env);

                var request = new { walletNumber };

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var payloadJson = JsonSerializer.Serialize(request, jsonOptions);
                var hashKey = GenerateHashKey(@params.ClientSecret, payloadJson);

                System.Diagnostics.Debug.WriteLine($"[NameEnquiry] payloadJson={payloadJson} hashKey={hashKey}");

                using var client = new HttpClient { BaseAddress = new Uri(@params.BaseUrl) };
                ExtractDefaultHeader(hashKey, @params.ClientId, client);

                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var httpResponse = await client.PostAsync("/v1/wallets/name-enquiry", content);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"[NameEnquiry] status={(int)httpResponse.StatusCode} body={responseContent}");

                var etransactResponse = JsonSerializer.Deserialize<NameEnquiryEtransactResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!httpResponse.IsSuccessStatusCode || etransactResponse == null || !etransactResponse.Success)
                {
                    var errorMessage = etransactResponse?.Message
                        ?? etransactResponse?.ResponseDescription
                        ?? HttpStatusCodeHelper.Describe(httpResponse.StatusCode)
                        ?? responseContent;

                    return WalletResponseService<NameEnquiryResult>.Failed(errorMessage, (int)httpResponse.StatusCode);
                }

                var result = new NameEnquiryResult
                {
                    AccountName = etransactResponse.Result?.AccountName,
                    PhoneNumber = etransactResponse.Result?.PhoneNumber,
                    EmailAddress = etransactResponse.Result?.EmailAddress,
                    EnquiryReference = etransactResponse.Result?.EnquiryReference
                };

                return WalletResponseService<NameEnquiryResult>.Success(
                    result,
                    etransactResponse.ResponseDescription ?? "Name enquiry completed successfully");
            }
            catch (HttpRequestException ex)
            {
                return WalletResponseService<NameEnquiryResult>.Failed(
                    "Unable to reach Etransact service: " + ex.Message, 503);
            }
            catch (Exception ex)
            {
                return WalletResponseService<NameEnquiryResult>.Failed(ex.Message, 500);
            }
        }

        public async Task<WalletResponseService<AccountValidationResponse>> BalanceEnquiryAsync(
            UserProfileResponse data,
            WalletProviderParameterModel[] parameters,
            string env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, env);
                var request = new { walletNumber = data.WalletNumber };

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var payloadJson = JsonSerializer.Serialize(request, jsonOptions);
                var hashKey = GenerateHashKey(@params.ClientSecret, payloadJson);

                System.Diagnostics.Debug.WriteLine($"[BalanceEnquiry] payloadJson={payloadJson} hashKey={hashKey}");

                using var client = new HttpClient { BaseAddress = new Uri(@params.BaseUrl) };
                ExtractDefaultHeader(hashKey, @params.ClientId, client);

                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var httpResponse = await client.PostAsync("/v1/wallets/balance-enquiry", content);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"[BalanceEnquiry] status={(int)httpResponse.StatusCode} body={responseContent}");

                var etransactResponse = JsonSerializer.Deserialize<BalanceEnquiryEtransactResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!httpResponse.IsSuccessStatusCode || etransactResponse == null || !etransactResponse.Success)
                {
                    var errorMessage = etransactResponse?.Message
                        ?? etransactResponse?.ResponseDescription
                        ?? HttpStatusCodeHelper.Describe(httpResponse.StatusCode)
                        ?? responseContent;

                    return WalletResponseService<AccountValidationResponse>.Failed(errorMessage, (int)httpResponse.StatusCode);
                }

                var result = new AccountValidationResponse
                {
                    AccountName = etransactResponse.Result?.AccountName,
                    balance = etransactResponse.Result?.Balance ?? 0,
                    ledgerBalance = etransactResponse.Result?.LedgerBalance ?? 0
                };

                return WalletResponseService<AccountValidationResponse>.Success(
                    result,
                    etransactResponse.ResponseDescription ?? "Balance enquiry completed successfully");
            }
            catch (HttpRequestException ex)
            {
                return WalletResponseService<AccountValidationResponse>.Failed(
                    "Unable to reach Etransact service: " + ex.Message, 503);
            }
            catch (Exception ex)
            {
                return WalletResponseService<AccountValidationResponse>.Failed(ex.Message, 500);
            }
        }

        public async Task<WalletResponseService<PaymentResponse>> DebitWalletAsync(
          WalletTopUpProviderRequest model,
            WalletProviderParameterModel[] parameters,
            string env)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.CustomerMac))
                {
                    return WalletResponseService<PaymentResponse>.Failed(
                        "customerMac (macChecksum) is required to sign a debit request.", 400);
                }

                var @params = await RetrieveParams(parameters, env);

                var request = new
                {
                    walletNumber = model.WalletNumber,
                    walletName = model.WalletName,
                    transactionReference = model.TransactionReference,
                    amount =model.Amount,
                    fee= @params.WalletFee,
                    stampDutyFee= @params.WalletStampDuty,
                    narration= model.Message
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var payloadJson = JsonSerializer.Serialize(request, jsonOptions);

                var hashKey = GenerateHashKey(@params.ClientSecret, payloadJson, model.CustomerMac);

                System.Diagnostics.Debug.WriteLine(
                    $"[DebitWallet] transactionReference={model.TransactionReference} payloadJson={payloadJson} hashKey={hashKey}");

                using var client = new HttpClient { BaseAddress = new Uri(@params.BaseUrl) };
                ExtractDefaultHeader(hashKey, @params.ClientId, client);

                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var httpResponse = await client.PostAsync("/v1/wallets/debit-wallet", content);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"[DebitWallet] status={(int)httpResponse.StatusCode} body={responseContent}");

                var etransactResponse = JsonSerializer.Deserialize<TransactionOperationEtransactResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!httpResponse.IsSuccessStatusCode || etransactResponse == null)
                {
                    return WalletResponseService<PaymentResponse>.Failed(
                        $"Debit outcome unknown ({HttpStatusCodeHelper.Describe(httpResponse.StatusCode)}). " +
                        $"Reconcile using transaction status before retrying. Raw: {responseContent}",
                        (int)httpResponse.StatusCode);
                }

                var result = new PaymentResponse
                {
                    Success = etransactResponse.Success,
                    ResponseCode = etransactResponse.ResponseCode,
                    ResponseDescription = etransactResponse.ResponseDescription
                };

                return etransactResponse.Success
                    ? WalletResponseService<PaymentResponse>.Success(
                        result, etransactResponse.ResponseDescription ?? "Debit completed successfully")
                    : WalletResponseService<PaymentResponse>.Failed(
                        etransactResponse.ResponseDescription ?? "Debit failed", (int)httpResponse.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                return WalletResponseService<PaymentResponse>.Failed(
                    "Unable to reach Etransact service, debit outcome unknown - reconcile via transaction status: "
                    + ex.Message, 503);
            }
            catch (Exception ex)
            {
                return WalletResponseService<PaymentResponse>.Failed(ex.Message, 500);
            }
        }

        public async Task<WalletResponseService<PaymentStatusResponse>> TransactionStatusAsync(
            string transactionReference,
            WalletProviderParameterModel[] parameters,
            string env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, env);

                var request = new { transactionReference };

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var payloadJson = JsonSerializer.Serialize(request, jsonOptions);
                var hashKey = GenerateHashKey(@params.ClientSecret, payloadJson);

                System.Diagnostics.Debug.WriteLine(
                    $"[TransactionStatus] payloadJson={payloadJson} hashKey={hashKey}");

                using var client = new HttpClient { BaseAddress = new Uri(@params.BaseUrl) };
                ExtractDefaultHeader(hashKey, @params.ClientId, client);

                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var httpResponse = await client.PostAsync("/v1/transactions/status", content);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"[TransactionStatus] status={(int)httpResponse.StatusCode} body={responseContent}");

                var etransactResponse = JsonSerializer.Deserialize<TransactionOperationEtransactResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!httpResponse.IsSuccessStatusCode || etransactResponse == null)
                {
                    return WalletResponseService<PaymentStatusResponse>.Failed(
                        $"{HttpStatusCodeHelper.Describe(httpResponse.StatusCode)}: {responseContent}",
                        (int)httpResponse.StatusCode);
                }

                var result = new PaymentStatusResponse
                {

                    Success = etransactResponse.Success,
                    ResponseCode = etransactResponse.ResponseCode,
                    ResponseDescription = etransactResponse.ResponseDescription
                };

                return WalletResponseService<PaymentStatusResponse>.Success(
                    result, etransactResponse.ResponseDescription ?? "Transaction status retrieved successfully");
            }
            catch (HttpRequestException ex)
            {
                return WalletResponseService<PaymentStatusResponse>.Failed(
                    "Unable to reach Etransact service: " + ex.Message, 503);
            }
            catch (Exception ex)
            {
                return WalletResponseService<PaymentStatusResponse>.Failed(ex.Message, 500);
            }
        }

        public async Task<WalletResponseService<PaymentResponse>> ReverseTransactionAsync(
            string transactionReference,
            WalletProviderParameterModel[] parameters,
            string env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, env);

                var request = new { transactionReference };

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var payloadJson = JsonSerializer.Serialize(request, jsonOptions);
                var hashKey = GenerateHashKey(@params.ClientSecret, payloadJson);

                System.Diagnostics.Debug.WriteLine(
                    $"[ReverseTransaction] payloadJson={payloadJson} hashKey={hashKey}");

                using var client = new HttpClient { BaseAddress = new Uri(@params.BaseUrl) };
                ExtractDefaultHeader(hashKey, @params.ClientId, client);

                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                var httpResponse = await client.PostAsync("/v1/transactions/reverse", content);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"[ReverseTransaction] status={(int)httpResponse.StatusCode} body={responseContent}");

                var etransactResponse = JsonSerializer.Deserialize<TransactionOperationEtransactResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!httpResponse.IsSuccessStatusCode || etransactResponse == null)
                {
                    return WalletResponseService<PaymentResponse>.Failed(
                        $"{HttpStatusCodeHelper.Describe(httpResponse.StatusCode)}: {responseContent}",
                        (int)httpResponse.StatusCode);
                }

                var result = new PaymentResponse
                {
                    Success = etransactResponse.Success,
                    ResponseCode = etransactResponse.ResponseCode,
                    ResponseDescription = etransactResponse.ResponseDescription
                };

                return etransactResponse.Success
                    ? WalletResponseService<PaymentResponse>.Success(
                        result, etransactResponse.ResponseDescription ?? "Reversal completed successfully")
                    : WalletResponseService<PaymentResponse>.Failed(
                        etransactResponse.ResponseDescription ?? "Reversal failed", (int)httpResponse.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                return WalletResponseService<PaymentResponse>.Failed(
                    "Unable to reach Etransact service: " + ex.Message, 503);
            }
            catch (Exception ex)
            {
                return WalletResponseService<PaymentResponse>.Failed(ex.Message, 500);
            }
        }
        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTopUpAsync(WalletTopUpProviderRequest request, WalletProviderParameterModel[] parameters, string Env)
        {
            throw new NotImplementedException();
        }

        public Task<WalletResponseService<PaymentResponse>> InitiateWalletTransferAsync(TransferRequest request, WalletProviderParameterModel[] parameters, string Env)
        {
            throw new NotImplementedException();
        }

      


    
    }

}
