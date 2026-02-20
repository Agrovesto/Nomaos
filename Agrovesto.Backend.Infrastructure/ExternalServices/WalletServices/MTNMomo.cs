using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Core.Models.Payment;
using Google.Apis.Drive.v3.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{
   public interface IMTNMomo 
    {
        Task<WalletResponseService<MomoPreApprovalResponse>> GetPreApproval(
        MomoPreApprovalRequest request,
        WalletProviderParameterModel[] parameters,
        string env);
        Task<WalletResponseService<MomoPaymentStatusResponse>> GetRequestToPayStatus(MomoPreApprovalRequest request, WalletProviderParameterModel[] parameters, string env);
        Task<WalletResponseService<bool>> RequestDirectDebitApproval(MomoPreApprovalRequest request, WalletProviderParameterModel[] parameters, string env);
    }

    public class MTNMomo : IGenericWalletService, IMTNMomo
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<MTNMomo> _log;
        private readonly IConfiguration _configuration;
        public MTNMomo(ILogger<MTNMomo> log, IHttpService httpService, IConfiguration configuration)
        {

            _httpService = httpService;
            _configuration = configuration;
            _log = log;
        }
        //    private HttpRequestMessage CreateRequest(
        //   HttpMethod method,
        //   string url,
        //   string accessToken,
        //   string referenceId,
        //   object body = null)
        //    {
        //        var @params= RetrieveParams()
        //        var request = new HttpRequestMessage(method, $"{@params.BaseUrl}{url}");

        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //        request.Headers.Add("Ocp-Apim-Subscription-Key", @params.SubscriptionKey);
        //        request.Headers.Add("X-Target-Environment", @params.TargetEnvironment);
        //        request.Headers.Add("X-Reference-Id", referenceId);

        //        if (body != null)
        //        {
        //            var json = JsonSerializer.Serialize(body);
        //            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        //        }

        //        return request;
        //    }

        public async Task<ProviderParams> RetrieveParams(WalletProviderParameterModel[] parameters, string Env)
        {
            var response = new ProviderParams();

            var _params = parameters.ToDictionary(x => x.Key);

            response.BaseUrl = _params["BaseUrl"].Value;
            response.SubscriptionApiKey = _params["ApiKey"].Value;
            response.SubscriptionKey = _params["SubscriptionKey"].Value;
            response.UserName = _params["UserName"].Value;
            response.TargetEnvironment = _params["TargetEnvironment"].Value;


            return response;

        }
        private string BasicAuthHeader(string username, string password)
        {
            var authString = $"{username}:{password}";
            var authBytes = Encoding.UTF8.GetBytes(authString);
            return Convert.ToBase64String(authBytes);
        }
        private async Task<MomoAuthResponse> GetProviderToken(ProviderParams @params, string authToken)
        {
            var headers = new Dictionary<string, string>
                {
                    { "Authorization",  $"Basic {authToken}"},
                    { "Ocp-Apim-Subscription-Key", @params.SubscriptionKey },
                    { "X-Target-Environment", @params.TargetEnvironment }
                };
            try
            {
                //var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                var request = await _httpService.Post($"{@params.BaseUrl}/collection/token", null, headers);
                var readResponse = await request.Content.ReadAsStringAsync();
                request.EnsureSuccessStatusCode();

                var response = JsonConvert.DeserializeObject<MomoAuthResponse>(readResponse);

                return response;


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
            }

        }

        private static Dictionary<string, string> GenerateHeader(ProviderParams @params, string BearerToken)
        {
            return new Dictionary<string, string>
                {
                    { "Authorization",  $"Bearer  {BearerToken}"},
                    { "Ocp-Apim-Subscription-Key", @params.SubscriptionKey },
                    { "X-Target-Environment", @params.TargetEnvironment }
                };
        }

        //    public async Task<WalletResponseService<PaymentResponse>> CollectAsync(CollectionRequest req)
        //    {
        //        try
        //        {
        //            var token = await GetProviderToken;
        //            var referenceId = req.ExternalReference ?? Guid.NewGuid().ToString();

        //            var body = new
        //            {
        //                amount = req.Amount,
        //                currency = req.Currency,
        //                externalId = referenceId,
        //                payer = new
        //                {
        //                    partyIdType = "MSISDN",
        //                    partyId = req.AccountNumber
        //                },
        //                payerMessage = req.Message,
        //                payeeNote = req.Message
        //            };

        //            var request = CreateRequest(
        //                HttpMethod.Post,
        //                "/collection/v1_0/requesttopay",
        //                token,
        //                referenceId,
        //                body);

        //            var response = await _httpService.SendAsync(request);

        //            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        //            {
        //                return WalletResponseService<PaymentResponse>.Pending(
        //                    new PaymentResponse
        //                    {
        //                        Reference = referenceId,
        //                        ProviderReference = referenceId,
        //                        Status = "Pending"
        //                    },
        //                    "Request to pay initiated",
        //                    202);
        //            }

        //            var content = await response.Content.ReadAsStringAsync();
        //            return WalletResponseService<PaymentResponse>.Failed(content, (int)response.StatusCode);
        //        }
        //        catch (Exception ex)
        //        {
        //            return WalletResponseService<PaymentResponse>.Failed(ex.Message, 500);
        //        }
        //    }

        //    public async Task<WalletResponseService<bool>> ValidateAccountAsync(string msisdn)
        //    {
        //        try
        //        {
        //            var token = await GetProviderToken;
        //            var referenceId = Guid.NewGuid().ToString();

        //            var request = CreateRequest(
        //                HttpMethod.Get,
        //                $"/collection/v1_0/accountholder/msisdn/{msisdn}/active",
        //                token,
        //                referenceId);

        //            var response = await _httpService.SendAsync(request);

        //            if (response.IsSuccessStatusCode)
        //                return WalletResponseService<bool>.Success(true);

        //            return WalletResponseService<bool>.Failed("Account not active");
        //        }
        //        catch (Exception ex)
        //        {
        //            return WalletResponseService<bool>.Failed(ex.Message, 500);
        //        }
        //    }

        //    public Task<WalletResponseService<object>> CreateWalletAsync(CreateWalletRequest request)
        //        => Task.FromResult(WalletResponseService<object>.Success(null, "MoMo wallet is MSISDN-based"));

        //    public Task<WalletResponseService<PaymentResponse>> DisburseAsync(DisbursementRequest request)
        //        => throw new NotImplementedException();

        //    public Task<WalletResponseService<PaymentStatusResponse>> GetTransactionStatusAsync(string reference)
        //        => throw new NotImplementedException();
        //}
        /**
         * BEGIN TRANSACTION

1. Fetch wallet WITH RowVersion
2. Validate balance (for debit)
3. Calculate new balance
4. Insert transaction (BalanceBefore/After)
5. Update wallet balance
6. SaveChanges()

COMMIT

         * */
        public async Task<WalletResponseService<MomoPaymentStatusResponse>> GetRequestToPayStatus(MomoPreApprovalRequest request, WalletProviderParameterModel[] parameters, string env)
        {
            var @params = await RetrieveParams(parameters, env);
            var basicAuth = BasicAuthHeader(@params.UserName, @params.SubscriptionApiKey);
            try
            {

                var token = await GetProviderToken(@params, basicAuth);
                var headers = GenerateHeader(@params, token.AccessToken);
                headers.Add("X-Reference-Id", request.Reference);

                var sendrequest = await _httpService.Get($"{@params.BaseUrl}/collection/v1_0/requesttopay/{request.Reference}", headers);
                var response = await sendrequest.Content.ReadAsStringAsync();


                var momoResponse = JsonSerializer.Deserialize<MomoPaymentStatusResponse>(response,
         new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (momoResponse == null)
                    return WalletResponseService<MomoPaymentStatusResponse>.Failed("Invalid response from provider");


                if (sendrequest.IsSuccessStatusCode)
                    return WalletResponseService<MomoPaymentStatusResponse>.Success(momoResponse);

                return WalletResponseService<MomoPaymentStatusResponse>.Failed("RequestToPay failed");


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _log.LogError(ex, $"Error Creating Wallet at FCMBWallet Service: {errorMsg}");
                throw;
            }


        }

        public async Task<WalletResponseService<PaymentResponse>> CollectAsync(CollectionRequest request, WalletProviderParameterModel[] parameters, string Env)
        {
            var @params = await RetrieveParams(parameters, Env);
            var basicAuth = BasicAuthHeader(@params.UserName, @params.SubscriptionApiKey);
            var callbackUrl = _configuration["CallbackUrl"];
            try
            {
                var token = await GetProviderToken(@params, basicAuth);
                var headers = GenerateHeader(@params, token.AccessToken);
                headers.Add("X-Reference-Id", request.Reference);
                headers.Add("X-Callback-Url", callbackUrl);

                var body = new
                {
                    amount = request.Amount,
                    currency = request.Currency,
                    externalId = request.Reference,
                    payer = new
                    {
                        partyIdType = "MSISDN",
                        partyId = request.AccountNumber
                    },          
                    payerMessage = request.Message,
                    payeeNote = request.Message,

                };
                var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                var sendRequest = await _httpService.Post(
    $"{@params.BaseUrl}/collection/v1_0/requesttopay",
    content,
    headers);

                if (sendRequest.IsSuccessStatusCode)
                {
                    var paymentResponse = new PaymentResponse
                    {
                        Status = sendRequest.StatusCode.ToString(),
                        Reference = request.Reference
                    };

                    return WalletResponseService<PaymentResponse>.Success(paymentResponse);
                }

                else
                {
                    var errorMessage = $"Request failed with status code {sendRequest.StatusCode} ({sendRequest.ReasonPhrase})";
                    return WalletResponseService<PaymentResponse>.Failed(errorMessage, (int)sendRequest.StatusCode);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _log.LogError(ex, $"Error Creating Wallet at FCMBWallet Service: {errorMsg}");
                throw;
            }

        }

        public async Task<WalletResponseService<object>> CreateWalletAsync(CreateWalletRequest request, WalletProviderParameterModel[] parameters, string Env)
        {

            throw new NotImplementedException();
        
           
        }

        public async Task<WalletResponseService<PaymentResponse>> DisburseAsync(DisbursementRequest request, WalletProviderParameterModel[] parameters, string Env)
        {
            var @params = await RetrieveParams(parameters, Env);
            var basicAuth = BasicAuthHeader(@params.UserName, @params.DisbursementApiKey);
            var callbackUrl = _configuration["CallbackUrl"];
            try
            {
                var token = await GetProviderToken(@params, basicAuth);
                var headers = GenerateHeader(@params, token.AccessToken);
                headers.Add("X-Reference-Id", request.Reference);
                headers.Add("X-Callback-Url", callbackUrl);

                var body = new
                {
                    amount = request.Amount,
                    currency = request.Currency,
                    externalId = request.Reference,
                    payee = new
                    {
                        partyIdType = "MSISDN",
                        partyId = request.AccountNumber
                    },
                    payerMessage = request.Message,
                    payeeNote = request.Message,

                };
                var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                var sendRequest = await _httpService.Post(
    $"{@params.BaseUrl}/disbursement/v1_0/transfer",
    content,
    headers);

                if (sendRequest.IsSuccessStatusCode)
                {
                    var paymentResponse = new PaymentResponse
                    {
                        Status = sendRequest.StatusCode.ToString(),
                        Reference = request.Reference
                    };

                    return WalletResponseService<PaymentResponse>.Success(paymentResponse);
                }

                else
                {
                    var errorMessage = $"Request failed with status code {sendRequest.StatusCode} ({sendRequest.ReasonPhrase})";
                    return WalletResponseService<PaymentResponse>.Failed(errorMessage, (int)sendRequest.StatusCode);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _log.LogError(ex, $"Error Creating Wallet at FCMBWallet Service: {errorMsg}");
                throw;
            }
        }

        public async Task<WalletResponseService<PaymentStatusResponse>> GetTransactionStatusAsync(string reference, WalletProviderParameterModel[] parameters, string Env)
        {
            var @params = await RetrieveParams(parameters, Env);
            var basicAuth = BasicAuthHeader(@params.UserName, @params.SubscriptionApiKey);
            try
            {

                var token = await GetProviderToken(@params, basicAuth);
                var headers = GenerateHeader(@params, token.AccessToken);
                headers.Add("X-Reference-Id", reference);
                var request = await _httpService.Get($"{@params.BaseUrl}/collection/v2_0/payment/{reference}", headers);
                var response = await request.Content.ReadAsStringAsync();

                var momoResponse = JsonSerializer.Deserialize<MomoPaymentStatusDto>(response,
          new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (momoResponse == null)
                    return WalletResponseService<PaymentStatusResponse>.Failed("Invalid response from provider");


                var genericResponse = new PaymentStatusResponse
                {
                    Status = momoResponse.Status,
                    Message = GetStatusMessage(momoResponse.Status),
                    Reference = momoResponse.ReferenceId,
                    TransactionId = momoResponse.FinancialTransactionId
                };

                if (request.IsSuccessStatusCode)
                    return WalletResponseService<PaymentStatusResponse>.Success(genericResponse);

                return WalletResponseService<PaymentStatusResponse>.Failed(genericResponse.Message);
            }
            catch (Exception ex)
            {
                return WalletResponseService<PaymentStatusResponse>.Failed(ex.Message, 500);
            }
        }

        public async Task<WalletResponseService<AccountValidationResponse>>
            ValidateAccountAsync(
                string accountNumber,
                WalletProviderParameterModel[] parameters,
                string Env)
        {
            var @params = await RetrieveParams(parameters, Env);
            var basicAuth = BasicAuthHeader(@params.UserName, @params.SubscriptionApiKey);

            try
            {
                var token = await GetProviderToken(@params, basicAuth);
                var headers = GenerateHeader(@params, token.AccessToken);

                var httpResponse = await _httpService.Get(
                    $"{@params.BaseUrl}/collection/v1_0/accountholder/msisdn/{accountNumber}/active",
                    headers);

                if (httpResponse.IsSuccessStatusCode)
                {
                    return WalletResponseService<AccountValidationResponse>.Success(
                        new AccountValidationResponse
                        {
                            AccountName = null,
                            WalletNumber = accountNumber
                        },
                        "Account is active"
                    );
                }

                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return WalletResponseService<AccountValidationResponse>.Failed(
                        "Wallet account not found or not active",
                        404);
                }

                var errorContent = await httpResponse.Content.ReadAsStringAsync();

                return WalletResponseService<AccountValidationResponse>.Failed(
                    $"Provider error: {httpResponse.StatusCode} - {errorContent}",
                    (int)httpResponse.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                return WalletResponseService<AccountValidationResponse>.Failed(
                    "Unable to reach MTN MoMo service: " + ex.Message,
                    503);
            }
            catch (Exception ex)
            {
                return WalletResponseService<AccountValidationResponse>.Failed(
                    ex.Message,
                    500);
            }
        }

        public async Task<WalletResponseService<MomoPreApprovalResponse>> GetPreApproval(MomoPreApprovalRequest request, WalletProviderParameterModel[] parameters, string env)
        {
            var @params = await RetrieveParams(parameters, env);
            var basicAuth = BasicAuthHeader(@params.UserName, @params.SubscriptionApiKey);
            try
            {

                var token = await GetProviderToken(@params, basicAuth);
                var headers = GenerateHeader(@params, token.AccessToken);

                var sendrequest = await _httpService.Get($"{@params.BaseUrl}/collection/v1_0/preapprovals/msisdn/{request.MSISDN}", headers);
                var response = await sendrequest.Content.ReadAsStringAsync();


                var momoResponse = JsonSerializer.Deserialize<MomoPreApprovalResponse>(response,
         new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (momoResponse == null)
                    return WalletResponseService<MomoPreApprovalResponse>.Failed("Invalid response from provider");


                if (sendrequest.IsSuccessStatusCode)
                    return WalletResponseService<MomoPreApprovalResponse>.Success(momoResponse);

                return WalletResponseService<MomoPreApprovalResponse>.Failed("PreApproval failed");


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _log.LogError(ex, $"Error Creating Wallet at FCMBWallet Service: {errorMsg}");
                throw;
            }


        }
        public async Task<WalletResponseService<bool>> RequestDirectDebitApproval(MomoPreApprovalRequest request, WalletProviderParameterModel[] parameters, string env)
        {
            var @params = await RetrieveParams(parameters, env);
            var basicAuth = BasicAuthHeader(@params.UserName, @params.SubscriptionApiKey);
            var callbackUrl = _configuration["CallbackUrl"];
            try
            {
                var token = await GetProviderToken(@params, basicAuth);
                var headers = GenerateHeader(@params, token.AccessToken);
                headers.Add("X-Reference-Id", request.Reference);
                headers.Add("X-Callback-Url", $"{callbackUrl}/mtnmomo");

                var body = new
                {
                    payer =  new 
                    {
                       partyIdType="MSISDN",
                       partyId = request.MSISDN
                    },
                  payerCurrency= request.Currency,
                  payerMessage = request.Message,
                    validityTime = ((int)TimeSpan.FromMinutes(60).TotalSeconds).ToString(),

            };
                var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                var sendRequest = await _httpService.Post(
    $"{@params.BaseUrl}/collection/v2_0/preapproval",
    content,
    headers);

                if (sendRequest.IsSuccessStatusCode)
                {
                    return WalletResponseService<bool>.Success(true);
                }
                else
                {
                    var errorMessage = $"Request failed with status code {sendRequest.StatusCode} ({sendRequest.ReasonPhrase})";
                    return WalletResponseService<bool>.Failed(errorMessage, (int)sendRequest.StatusCode);
                }

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _log.LogError(ex, $"Error Creating Wallet at FCMBWallet Service: {errorMsg}");
                throw;
            }
        }
        private string GetStatusMessage(string status)
        {
            return status switch
            {
                "SUCCESSFUL" => "Transaction successful",
                "PENDING" => "Transaction pending",
                "FAILED" => "Transaction failed",
                _ => "Unknown status"
            };
        }
    }
}
