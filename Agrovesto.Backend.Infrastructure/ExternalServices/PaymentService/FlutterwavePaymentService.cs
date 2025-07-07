using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Payment.Flutterwave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.PaymentService
{
    public class FlutterwavePaymentService : IFlutterwaveService
    {
        private readonly IPaymentProviderRepository _providerRepo;
        private readonly IHttpService _httpService;
        public FlutterwavePaymentService(IHttpService httpService, IPaymentProviderRepository providerRepo)
        {
            _providerRepo = providerRepo;
            _httpService = httpService;
        }
        public async Task<PaymentResponseModel> InitializePaymet(PaymentRequestModel model, string Env)
        {
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == "FLW").FirstOrDefault().ProviderId;
            var parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            var _params = parameters.ToDictionary(x => x.Key);

            var url = _params["Url"].Value;
            var secretkey = _params["Secret_key"].Value;
            var publicKey = _params["Public_key"].Value;
            var redirect_Url = _params["topUpredirect_Url"].Value;
            var headers = new Dictionary<string, string>
            {
                {"Authorization", $"Bearer {secretkey}" }
            };

            model.redirect_url = redirect_Url;
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                var postPaymentInfo = await _httpService.Post($"{url}/payments", content, headers);

                var readResponse = await postPaymentInfo.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<PaymentResponseModel>(readResponse);

                return response;

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new PaymentResponseModel
                {
                    status = "500",
                    message = "Something went wrong while connecting to Flutterwave"
                };
            }
        }

        public async Task<PaymentTransferResponseModel> Transfer(PaymentTransferRequest model, string Env)
        {
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == "FLW").FirstOrDefault().ProviderId;

            var parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            var _params = parameters.ToDictionary(x => x.Key);

            var url = _params["Url"].Value;
            var secretkey = _params["Secret_key"].Value;
            var publicKey = _params["Public_key"].Value;
            var redirect_Url = _params["topUpredirect_Url"].Value;
            var headers = new Dictionary<string, string>
            {
                {"Authorization", $"Bearer {secretkey}" }
            };
            model.callback_url = redirect_Url;
            try
            {

                var client = new HttpClient();
                PaymentTransferResponseModel deserializeResponse;
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretkey}");
                var data = JsonConvert.SerializeObject(model);
                var postPaymentInfo = await client.PostAsync($"{url}/transfers", new StringContent(data, Encoding.UTF8, "application/json"));

                var readResponse = await postPaymentInfo.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<PaymentTransferResponseModel>(readResponse);

                return response;

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new PaymentTransferResponseModel
                {
                    status = "500",
                    message = "Something went wrong while connecting to Flutterwave"
                };
            }
        }


        public async Task<PaymentTransferResponseModel> FetchTransfer(long id, string Env)
        {
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == "FLW").FirstOrDefault().ProviderId;

            var parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            var _params = parameters.ToDictionary(x => x.Key);

            var url = _params["Url"].Value;
            var secretkey = _params["Secret_key"].Value;
            var publicKey = _params["Public_key"].Value;
            var redirect_Url = _params["topUpredirect_Url"].Value;
            var headers = new Dictionary<string, string>
            {
                {"Authorization", $"Bearer {secretkey}" }
            };
            var requestUrl = $"{url}/transfers/{id}";
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretkey}");

                var httpResponseMessage = await client.GetAsync(requestUrl);
                var readResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<PaymentTransferResponseModel>(readResponse);

                return response;

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new PaymentTransferResponseModel
                {
                    status = "500",
                    message = "Something went wrong while connecting to Flutterwave "
                };
            }
        }

        public async Task<PaymentVerificationResponseModel> VerifyTransaction(int transactionId, string Env)
        {
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == "FLW").FirstOrDefault().ProviderId;

            var parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            var _params = parameters.ToDictionary(x => x.Key);

            var url = _params["Url"].Value;
            var secretkey = _params["Secret_key"].Value;
            var publicKey = _params["Public_key"].Value;
            var redirect_Url = _params["topUpredirect_Url"].Value;
            var headers = new Dictionary<string, string>
            {
                {"Authorization", $"Bearer {secretkey}" }
            };
            var requestUrl = $"{url}/transactions/{transactionId}/verify";
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretkey}");

                var httpResponseMessage = await client.GetAsync(requestUrl);
                var readResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<PaymentVerificationResponseModel>(readResponse);

                return response;

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new PaymentVerificationResponseModel
                {
                    status = "500",
                    message = "Something went wrong while connecting to Flutterwave "
                };
            }
        }

        public async Task<BankResponseModel> GetBanks(string Env, string countryCode = "NG")
        {
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == "FLW").FirstOrDefault().ProviderId;

            var parameters = await _providerRepo.GetParametersByProvider(providerId, "Live");
            var _params = parameters.ToDictionary(x => x.Key);

            var url = _params["Url"].Value;
            var secretkey = _params["Secret_key"].Value;
            var publicKey = _params["Public_key"].Value;
            var headers = new Dictionary<string, string>
            {
                {"Authorization", $"Bearer {secretkey}" }
            };
            var requestUrl = $"{url}/banks/{countryCode}";
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretkey}");

                var httpResponseMessage = await client.GetAsync(requestUrl);
                var readResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<BankResponseModel>(readResponse);

                response.data = response.data.OrderBy(r => r.name).ToArray();

                return response;

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new BankResponseModel
                {
                    status = "500",
                    message = "Something went wrong while connecting to Flutterwave "
                };
            }

        }


        public async Task<PaymentTransferResponseModel> ResolveAccount(string accountNumber, string bankCode, string Env)
        {
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == "FLW").FirstOrDefault().ProviderId;

            var parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            var _params = parameters.ToDictionary(x => x.Key);

            var url = _params["Url"].Value;
            var secretkey = _params["Secret_key"].Value;
            var publicKey = _params["Public_key"].Value;
            var redirect_Url = _params["topUpredirect_Url"].Value;
            var headers = new Dictionary<string, string>
            {
                {"Authorization", $"Bearer {secretkey}" }
            };
            var model = new
            {
                account_number = accountNumber,
                account_bank = bankCode,
            };
            try
            {

                var client = new HttpClient();
                PaymentTransferResponseModel deserializeResponse;
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretkey}");
                var data = JsonConvert.SerializeObject(model);
                var postPaymentInfo = await client.PostAsync($"{url}/accounts/resolve", new StringContent(data, Encoding.UTF8, "application/json"));

                var readResponse = await postPaymentInfo.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<PaymentTransferResponseModel>(readResponse);

                return response;

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new PaymentTransferResponseModel
                {
                    status = "500",
                    message = "Something went wrong while connecting to Flutterwave"
                };
            }
        }

    }

    public class FlutterBank
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

}

