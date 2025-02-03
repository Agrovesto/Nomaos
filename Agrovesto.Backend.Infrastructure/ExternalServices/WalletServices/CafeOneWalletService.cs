using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{

    public class CafeOneWalletService : ICafeOneWalletService
    {
        private readonly IWalletProviderRepository _providerRepo;
        private readonly IHttpService _httpService;
        public CafeOneWalletService(IHttpService httpService, IWalletProviderRepository providerRepo)
        {
            _providerRepo = providerRepo;
            _httpService = httpService;
        }
        public async Task<WalletResponseData> CreateWallet(string WalletName, string Env)
        {
            var @params = await RetrieveParams(Env);

            var model = new
            {
                organizationId = @params.OrganizationId,
                ownerId = @params.OwnerId,
                accountName = WalletName,
                currency = "NGN"
            };
            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.Url}/wallet/create", content, headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<WalletResponseData>>(readResponse);
                if (response.StatusCode == 200)
                {

                    return response.Data;
                }
                else
                {
                    return new WalletResponseData();
                }

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new WalletResponseData
                {

                };
            }
        }

        public async Task<CafeOneParams> RetrieveParams(string Env)
        {
            var response = new CafeOneParams();
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == "CafeOne").FirstOrDefault().ProviderId;
            var parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            var _params = parameters.ToDictionary(x => x.Key);

            response.Url = _params["url"].Value;
            response.SecretKey = _params["secretkey"].Value;
            response.PublicKey = _params["publickey"].Value;
            response.OrganizationId = _params["organizationId"].Value;
            response.OwnerId = _params["ownerId"].Value;
            response.Email = _params["email"].Value;
            response.Password = _params["password"].Value;
            response.accountNumber = _params["accountNumber"].Value;


            return response;

        }
        public async Task<CafeoneDataModel> CreateCustomer(CafeoneCustomerRequest model, string Env)
        {
            var @params = await RetrieveParams(Env);
            DateTime date = DateTime.ParseExact(model.dob, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            string dobOfBirth = date.ToString("dd-MM-yyyy");
            model.dob = dobOfBirth;

            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.Url}/customers", content, headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeoneResponseModel>(readResponse);
                if (response.StatusCode == 200)
                {

                    return response.Data;
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
        public async Task<WalletResponseData> CreateCustomerWallet(string userId, string WalletName, string Env)
        {
            var @params = await RetrieveParams(Env);

            var model = new
            {
                organizationId = @params.OrganizationId,
                ownerId = userId,
                accountName = WalletName,
                currency = "NGN"
            };
            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.Url}/wallet/create", content, headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<WalletResponseData>>(readResponse);
                if (response.StatusCode == 200)
                {

                    return response.Data;
                }
                else
                {
                    return new WalletResponseData();
                }

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new WalletResponseData
                {

                };
            }
        }

        public async Task<WalletResponseData> GetWallet(string env)
        {
            var @params = await RetrieveParams(env);


            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var request = await _httpService.Get<HttpResponseMessage>($"{@params.Url}/wallet/list/${@params.OrganizationId}", headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<WalletResponseData>>(readResponse);



                return response.Data;


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
            }

        }


        public async Task<CafeOneWalletProvider> GetWalletbyId(string walletId, string env)
        {
            var @params = await RetrieveParams(env);


            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var request = await _httpService.Get<HttpResponseMessage>($"{@params.Url}/wallet/${walletId}", headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<CafeOneData>>(readResponse);



                return response.Data.WalletProviders.FirstOrDefault();


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
            }

        }


        public async Task<CafeOneWalletProvider> GetUserAccount(string internalAccountId, string env)
        {
            var @params = await RetrieveParams(env);


            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var request = await _httpService.Get<HttpResponseMessage>($"{@params.Url}/wallet/providers/${internalAccountId}", headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<AccountNumbers>>(readResponse);



                return response.Data.Providers.FirstOrDefault();


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
            }

        }
        public async Task<WalletResponseData> AgrovestoToUserWalletTransfer(string accountNumber, decimal amount, string reference, string Env)
        {
            var @params = await RetrieveParams(Env);

            var model = new
            {
                organizationId = @params.OrganizationId,
                ownerId = @params.OwnerId,
                fromAccountNumber = @params.accountNumber,
                toAccountNumber = accountNumber,
                amount = amount,
                memo = "Agrovesto Wallet Transfer",
                yourTnxRef = reference,

            };
            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.PublicKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.Url}/wallet/transfer", content, headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<WalletResponseData>>(readResponse);
                if (response.StatusCode == 200)
                {

                    return response.Data;
                }
                else
                {
                    return new WalletResponseData();
                }

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new WalletResponseData
                {

                };
            }
        }

        //

        public async Task<WalletResponseData> UserWalletToAgrovestoTransfer(string reference, string ownerId, decimal amount, string fromAccount, string Env)
        {
            var @params = await RetrieveParams(Env);

            var model = new
            {
                organizationId = @params.OrganizationId,
                ownerId = ownerId,
                fromAccountNumber = fromAccount,
                toAccountNumber = @params.accountNumber,
                amount = amount,
                memo = "User Wallet Transfer",
                yourTnxRef = reference,

            };

            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.PublicKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.Url}/wallet/transfer", content, headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<WalletResponseData>>(readResponse);
                if (response.StatusCode == 200)
                {

                    return response.Data;
                }
                else
                {
                    return new WalletResponseData();
                }

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new WalletResponseData
                {

                };
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
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<CafeOneAuthResponse>>(readResponse);



                return response.Data;


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
            }

        }
        public async Task<CafeOneExternalTransferResponse> UserWalletToExternalBankTransfer(ExternalBankTransferModel extmodel, string Env)
        {
            var @params = await RetrieveParams(Env);

            var model = new
            {
                amount = extmodel.amount,
                fromWalletAccountNumber = extmodel.fromWalletAccountNumber, //internal account number
                toExternaAccountNumber = extmodel.toExternalAccountNumber,
                accountName = extmodel.accountName,
                bankName = extmodel.bankName,
                bankCode = extmodel.bankCode,
                memo = extmodel.memo,
                ownerId = extmodel.organizationId,
                organizationId = @params.OrganizationId,
                yourTnxRef = extmodel.yourTnxRef
            };

            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.PublicKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.Url}/wallet-providers/core/external/transfer", content, headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<CafeOneExternalTransferResponse>>(readResponse);
                if (response.StatusCode == 200)
                {

                    return response.Data;
                }
                else
                {
                    return new CafeOneExternalTransferResponse();
                }

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new CafeOneExternalTransferResponse
                {

                };
            }
        }


        public async Task<List<CafeOneBankData>> GetBanks(string env)
        {
            var @params = await RetrieveParams(env);


            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var request = await _httpService.Get<CafeOneBankDataResponse>($"{@params.Url}/wallet-providers/core/banks", headers);
                // request.EnsureSuccessStatusCode();
                //var readResponse = await request.Content.ReadAsStringAsync();
                //var response = JsonConvert.DeserializeObject<AccountResolutionResponse<CafeOneBankDataResponse>>(readResponse);



                return request.data;


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
            }

        }

        public async Task<AccountResolutionResolutionResponseData> ResolveUserAccount(string accountNumber, string bankCode, string Env)
        {
            var @params = await RetrieveParams(Env);

            var model = new
            {
                accountNumber = accountNumber,
                bankCode = bankCode,
            };

            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.PublicKey}"},
                    { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.Url}/wallet-providers/core/resolve/account-name", content, headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<AccountResolutionResponse<AccountResolutionResolutionResponseData>>>(readResponse);
                if (response.StatusCode == 200)
                {

                    return response.Data.data;
                }
                else
                {
                    return new AccountResolutionResolutionResponseData();
                }

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return new AccountResolutionResolutionResponseData
                {

                };
            }
        }


        //get wallet   .....https://dev-api.c-one.ng/wallet/list/{ownerId}
        //get wallet by id   .....https://dev-api.c-one.ng/wallet/getById/{walletId}

        //wallet transfer   ....https://dev-api.c-one.ng/wallet/transfer

        // wallet to my bank account transfer ....https://dev-api.c-one.ng/wallet-providers/core/external/transfer
    }
}

