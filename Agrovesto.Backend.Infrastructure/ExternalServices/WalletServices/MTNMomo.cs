using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{


    public class MTNMomo : IGenericWalletService
    {
        //    private readonly IHttpService _httpService;
        //    private readonly ILogger<MTNMomo> _log;
        //    public MTNMomo(ILogger<MTNMomo> log, IHttpService httpService)
        //    {

        //        _httpService = httpService;

        //        _log = log;
        //    }
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

        //    public async Task<CafeOneParams> RetrieveParams(WalletProviderParameterModel[] parameters, string Env)
        //    {
        //        var response = new CafeOneParams();

        //        var _params = parameters.ToDictionary(x => x.Key);

        //        response.Url = _params["url"].Value;
        //        response.SecretKey = _params["secretkey"].Value;
        //        response.PublicKey = _params["publickey"].Value;
        //        response.OrganizationId = _params["organizationId"].Value;
        //        response.OwnerId = _params["ownerId"].Value;
        //        response.Email = _params["email"].Value;
        //        response.Password = _params["password"].Value;
        //        response.accountNumber = _params["accountNumber"].Value;
        //        response.customerTypeId = _params["customerTypeId"].Value;
        //        response.countryId = _params["countryId"].Value;
        //        response.currencyId = _params["currencyId"].Value;
        //        response.currency = _params["currency"].Value;
        //        response.debitdirection = _params["debitdirection"].Value;
        //        response.creditdirection = _params["creditdirection"].Value;
        //        response.productCredit = _params["productCredit"].Value;
        //        response.productDebit = _params["productDebit"].Value;
        //        response.payoutUrl = _params["payoutUrl"].Value;


        //        return response;

        //    }

        //    private async Task<CafeOneAuthResponse> GetProviderToken(string url, string email, string password, string key)
        //    {
        //        var headers = new Dictionary<string, string>
        //        {
        //            { "x-api-key",  $"Bearer {key}"}
        //        };
        //        var model = new
        //        {
        //            email = email,
        //            password = password
        //        };
        //        try
        //        {
        //            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
        //            var request = await _httpService.Post($"{url}/auth/login", content, headers);
        //            var readResponse = await request.Content.ReadAsStringAsync();
        //            request.EnsureSuccessStatusCode();

        //            var response = JsonConvert.DeserializeObject<Core.Models.Common.CafeOnWalletResponse<CafeOneAuthResponse>>(readResponse);



        //            return response.Data;


        //        }
        //        catch (Exception ex)
        //        {
        //            string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        //            return null;
        //        }

        //    }

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
        public Task<WalletResponseService<PaymentResponse>> CollectAsync(CollectionRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<WalletResponseService<object>> CreateWalletAsync(CreateWalletRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<WalletResponseService<PaymentResponse>> DisburseAsync(DisbursementRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<WalletResponseService<PaymentStatusResponse>> GetTransactionStatusAsync(string reference)
        {
            throw new NotImplementedException();
        }

        public Task<WalletResponseService<bool>> ValidateAccountAsync(string accountNumber)
        {
            throw new NotImplementedException();
        }
    }
}
