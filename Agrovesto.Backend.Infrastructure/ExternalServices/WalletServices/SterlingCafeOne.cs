using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Core.V2.Onboarding;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{
    public interface ISterlingCafeOne
    {
      
    }

    public class SterlingCafeOne 
        //: IGenericWalletService,
        //ISterlingCafeOne
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<CafeOneWalletService> _log;
        public SterlingCafeOne(ILogger<CafeOneWalletService> log, IHttpService httpService)
        {
            _httpService = httpService;
            _log = log;
        }
        public async Task<CafeOneParams> RetrieveParams(WalletProviderParameterModel[] parameters, string Env)
        {
            var response = new CafeOneParams();

            var _params = parameters.ToDictionary(x => x.Key);

            response.Url = _params["url"].Value;
            response.SecretKey = _params["secretkey"].Value;
            response.PublicKey = _params["publickey"].Value;
            response.OrganizationId = _params["organizationId"].Value;
            response.OwnerId = _params["ownerId"].Value;
            response.Email = _params["email"].Value;
            response.Password = _params["password"].Value;
            response.accountNumber = _params["accountNumber"].Value;
            response.customerTypeId = _params["customerTypeId"].Value;
            response.countryId = _params["countryId"].Value;
            response.currencyId = _params["currencyId"].Value;
            response.currency = _params["currency"].Value;
            response.debitdirection = _params["debitdirection"].Value;
            response.creditdirection = _params["creditdirection"].Value;
            response.productCredit = _params["productCredit"].Value;
            response.productDebit = _params["productDebit"].Value;
            response.payoutUrl = _params["payoutUrl"].Value;

            return response;

        }

        public Task<WalletResponseService<PaymentResponse>> CollectAsync(CollectionRequest request, WalletProviderParameterModel[] parameters, string Env)
        {
            throw new NotImplementedException();
        }

        public Task<WalletResponseService<object>> CreateWalletAsync(CreateWalletRequest request, WalletProviderParameterModel[] parameters, string Env)
        {
            throw new NotImplementedException();
        }

        public Task<WalletResponseService<PaymentResponse>> DisburseAsync(TransferRequest request, WalletProviderParameterModel[] parameters, string Env)
        {
            throw new NotImplementedException();
        }

        public Task<WalletResponseService<PaymentStatusResponse>> GetTransactionStatusAsync(string reference, WalletProviderParameterModel[] parameters, string Env)
        {
            throw new NotImplementedException();
        }

        public async Task<WalletResponseService<AccountValidationResponse>> ValidateAccountAsync(UserProfileResponse data, WalletProviderParameterModel[] parameters, string Env)
        {
            throw new NotImplementedException();
        }
        public async Task<CafeoneDataModel> CreateCustomer(CafeoneCustomerRequest model, WalletProviderParameterModel[] parameters, string Env)
        {
            try
            {
                var @params = await RetrieveParams(parameters, Env);
                PopulateRequestModel(model, @params);

                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);

                var headers = new Dictionary<string, string>
                {
                    { "x-api-key", @params.SecretKey }
                    // { "Authorization", $"Bearer {token.AccessToken}" }
                };

                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                var httpResponse = await _httpService.Post($"{@params.Url}/api/v1/customers/add", content, headers);
                var responseString = await httpResponse.Content.ReadAsStringAsync();

                var response = JsonConvert.DeserializeObject<CafeoneResponseModel<CafeoneDataModel>>(responseString);
                if (response == null) return null;

                if (IsDuplicateCustomerMessage(response.message))
                {
                    return await HandleDuplicateCustomer(model, @params, response,parameters, Env);
                }

                if (response.success)
                {
                    await VerifyKYC(model, @params, response,parameters, Env);
                    return response.data;
                }

                return null;
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                // Log error: _logService.Error(ex, $"CreateCustomer Error: {errorMsg}");
                return null;
            }
        }

        private void PopulateRequestModel(CafeoneCustomerRequest model, CafeOneParams @params)
        {
            model.CountryId = @params.countryId;
            model.CustomerTypeId = @params.customerTypeId;
        }

        private bool IsDuplicateCustomerMessage(string message)
        {
            return message != null && message.Contains("A customer with these credentials already exist");
        }

        private async Task<CafeoneDataModel> HandleDuplicateCustomer(
            CafeoneCustomerRequest model,
            CafeOneParams @params,
            CafeoneResponseModel<CafeoneDataModel> response,
            WalletProviderParameterModel[] parameters,
            string env)
        {
            var customers = await GetCustomers(parameters, env);
            var matchedCustomer = customers.FirstOrDefault(x => x.MobileNumber == model.mobileNumber);
            if (matchedCustomer != null)
            {
                response.data = matchedCustomer;
                await VerifyKYC(model, @params, response, parameters, env);


                return response.data;
            }

            return null;
        }
        public async Task<List<CafeoneDataModel>> GetCustomers(WalletProviderParameterModel[] parameters, string env)
        {
            var @params = await RetrieveParams(parameters, env);


            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                  //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var request = await _httpService.Get($"{@params.Url}/api/v1/customers/get/all", headers);
                // request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeoneResponseModel<List<CafeoneDataModel>>>(readResponse);



                return response.data;


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
            }

        }

        private async Task VerifyKYC(CafeoneCustomerRequest model, CafeOneParams @params, CafeoneResponseModel<CafeoneDataModel>? response, WalletProviderParameterModel[] parameters, string env)
        {
            var updateKYC = await UpdateCustomerKyc(new CafeOneCustomerKYCRequest
            {
                firstName = model.firstName,
                CustomerId = response.data.Id,
                dob = model.dob,
                lastName = model.lastName,
                Nin = model.nin
            }, @params, parameters, env);
        }
        public async Task<bool> UpdateCustomerKyc(CafeOneCustomerKYCRequest model, CafeOneParams @params, WalletProviderParameterModel[] parameters, string env)
        {
            if (@params == null)
            {
                @params = await RetrieveParams(parameters, env);
            }

            try
            {
                var request = new
                {
                    firstname = model.firstName,
                    lastname = model.lastName,
                    dob = model.dob,
                };
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                   // { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                var sendrequest = await _httpService.Post($"{@params.Url}/api/v1/customers/kyc/customer/nin?customerId={model.CustomerId}&nin={model.Nin}&verify={1}", content, headers);
                var readResponse = await sendrequest.Content.ReadAsStringAsync();
                sendrequest.EnsureSuccessStatusCode();

                return true;
                //var response = JsonConvert.DeserializeObject<CafeoneResponseModel<bool>>(readResponse);
                //if (response.success)
                //{

                //    return true;
                //}
                //else
                //{
                //    return false;
                //}

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return false;
            }
        }

        public async Task<CafeOneCustomerWalletResponse> CreateCustomerWallet(CafeOneCustomerWalletCreationRequest model, WalletProviderParameterModel[] parameters, string Env)
        {
            var @params = await RetrieveParams(parameters, Env);


            try
            {
                model.CurrencyId = @params.currencyId;
                var data = new
                {
                    customerId = model.CustomerId,
                    name = model.Name,
                    currencyId = model.CurrencyId,
                };

                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                    //{ "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.Url}/api/v1/wallets/add", content, headers);
                var readResponse = await request.Content.ReadAsStringAsync();


                var response = JsonConvert.DeserializeObject<CafeoneResponseModel<CafeOneCustomerWalletResponse>>(readResponse);

                // request.EnsureSuccessStatusCode();

                if (response.success)
                {
                    //Step 2

                    return response.data;
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return null;
            }
        }
        public async Task<CafeOneCustomerWalletResponse> RetrieveCustomerWallet(string customerId, string walletId, WalletProviderParameterModel[] parameters, string Env)
        {
            var @params = await RetrieveParams(parameters, Env);

            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                   // { "Authorization",  $"Bearer {token.AccessToken}"}
                };

                var request = await _httpService.Get($"{@params.Url}/api/v1/wallets/get/customer/{customerId}/wallet/{walletId}", headers);
                var readResponse = await request.Content.ReadAsStringAsync();
                request.EnsureSuccessStatusCode();

                var response = JsonConvert.DeserializeObject<CafeoneResponseModel<CafeOneCustomerWalletResponse>>(readResponse);
                if (response.success)
                {
                    return response.data;
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return null;
            }

        }
        private async Task<CafeOneAuthResponse> GetProviderToken(string url, string email, string password, string key)
        {
            var headers = new Dictionary<string, string>
            {
                { "x-api-key",  $"Bearer {key}"}
            };
            var model = new
            {
                email = email,
                password = password
            };
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                var request = await _httpService.Post($"{url}/auth/login", content, headers);
                var readResponse = await request.Content.ReadAsStringAsync();
                request.EnsureSuccessStatusCode();

                var response = JsonConvert.DeserializeObject<Core.Models.Common.CafeOnWalletResponse<CafeOneAuthResponse>>(readResponse);



                return response.Data;


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
            }

        }


    }
}
