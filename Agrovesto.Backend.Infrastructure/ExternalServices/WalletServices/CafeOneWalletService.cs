using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
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
        private readonly IWalletRepository _walletRepository;
        public CafeOneWalletService(IHttpService httpService, IWalletProviderRepository providerRepo, IWalletRepository walletRepository)
        {
            _providerRepo = providerRepo;
            _httpService = httpService;
            _walletRepository = walletRepository;
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
            response.customerTypeId = _params["customerTypeId"].Value;
            response.countryId = _params["countryId"].Value;
            response.currencyId = _params["currencyId"].Value;
            response.currency = _params["currency"].Value;
            response.debitdirection = _params["debitdirection"].Value;
            response.creditdirection = _params["creditdirection"].Value;
            response.productCredit = _params["productCredit"].Value;
            response.productDebit = _params["productDebit"].Value;


            return response;

        }


        #region Customer Account Creation
        //Step 1
        public async Task<CafeoneDataModel> CreateCustomer(CafeoneCustomerRequest model, string Env)
        {
            var @params = await RetrieveParams(Env);
           

            try
            {
                model.CountryId = @params.countryId;
                model.CustomerTypeId = @params.customerTypeId;
             
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                   // { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.Url}/customers/add", content, headers);
                var readResponse = await request.Content.ReadAsStringAsync();
                request.EnsureSuccessStatusCode();

                var response = JsonConvert.DeserializeObject<CafeoneResponseModel<CafeoneDataModel>>(readResponse);
                if (response.success)
                {
                    //Step 2
                    await VerifyKYC(model, @params, response, Env);
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

        private async Task VerifyKYC(CafeoneCustomerRequest model, CafeOneParams @params, CafeoneResponseModel<CafeoneDataModel>? response, string env)
        {
            var updateKYC = await UpdateCustomerKyc(new CafeOneCustomerKYCRequest
            {
                firstName = model.firstName,
                CustomerId = response.data.Id,
                dob = model.dob,
                lastName = model.lastName,
                Nin = model.nin
            }, @params, env);
        }

        public async Task<bool> UpdateCustomerKyc(CafeOneCustomerKYCRequest model, CafeOneParams @params, string env)
        {
            if(@params == null)
            {
                @params = await RetrieveParams(env);
            }

            try
            {
                var request = new
                {
                    firstname= model.firstName,
                    lastname= model.lastName,
                    dob=model.dob,
                };
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                   // { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                var sendrequest = await _httpService.Post($"{@params.Url}/customers/kyc/customer/nin?customerId={model.CustomerId}&nin={model.Nin}&verify={1}", content, headers);
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

        public async Task<CafeOneCustomerWalletResponse> CreateCustomerWallet(CafeOneCustomerWalletCreationRequest model, string Env)
        {
            var @params = await RetrieveParams(Env);
           

            try
            {
                model.CurrencyId = @params.currencyId;
                var data = new
                {
                    customerId= model.CustomerId,
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

                var request = await _httpService.Post($"{@params.Url}/wallets/add", content, headers);
                var readResponse = await request.Content.ReadAsStringAsync();
                request.EnsureSuccessStatusCode();

                var response = JsonConvert.DeserializeObject<CafeoneResponseModel<CafeOneCustomerWalletResponse>>(readResponse);
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
        public async Task<CafeOneCustomerWalletResponse> RetrieveCustomerWallet(string customerId, string walletId, string Env)
        {
            var @params = await RetrieveParams(Env);        

            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                   // { "Authorization",  $"Bearer {token.AccessToken}"}
                };

                var request = await _httpService.Get($"{@params.Url}/wallets/get/customer/{customerId}/wallet/{walletId}",  headers);
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


        #endregion Customer Account Creation
        public async Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> TransferFromUserAccountToAgrovestoAccount(WalletToWalletTransferRequestModel request, string Env)
        {
            var @params = await RetrieveParams(Env);

            var model = new
            {
                organizationId = @params.OrganizationId,
                productId = @params.productDebit,
                amount = request.Amount,
                direction = @params.debitdirection,
                currency = @params.currency,
                walletId = request.WalletId,
                transactionReference = request.TransactionReference,
                customerId = request.CustomerId,
                remarks = request.Remarks,
                batchReference = request.BatchReference,
                accountNumber = @params.accountNumber,

            };

            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.PublicKey}"},
                  //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var query = await _httpService.Post($"{@params.Url}/wallets/wallet/transaction/account-number/post", content, headers);
                var readResponse = await query.Content.ReadAsStringAsync();
                query.EnsureSuccessStatusCode();

                var response = JsonConvert.DeserializeObject<CafeoneResponseModel<CafeOneCustomerWalletResponse>>(readResponse);
                  response.success = true;
                if (response.success)
                {

                    return response;
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


        public async Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> TransferFromUserAccountToBusinessAccount(WalletToWalletTransferRequestModel request, string Env)
        {
            var @params = await RetrieveParams(Env);

            var model = new
            {
                organizationId = @params.OrganizationId,
                productId = @params.productDebit,
                amount = request.Amount,
                direction = @params.debitdirection,
                currency = @params.currency,
                walletId = request.WalletId,
                transactionReference = request.TransactionReference,
                customerId = request.CustomerId,
                remarks = request.Remarks,
                batchReference = request.BatchReference,
                accountNumber = request.AccountNumber

            };

            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.PublicKey}"},
                  //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var query = await _httpService.Post($"{@params.Url}/wallets/wallet/transaction/account-number/post", content, headers);
                var readResponse = await query.Content.ReadAsStringAsync();
                query.EnsureSuccessStatusCode();
             
                var response = JsonConvert.DeserializeObject<CafeoneResponseModel<CafeOneCustomerWalletResponse>>(readResponse);

                
                if (response.success)
                {

                    return response;
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
        public async Task<CafeOnWalletResponse<WalletTransferResult>> UserWalletToBusinessWalletTransfer(WalletToWalletTransferRequest request)
        {
            var @params = await RetrieveParams(request.Env);

            var model = new
            {
                organizationId = @params.OrganizationId,
                ownerId = request.OwnerId,
                fromAccountNumber = request.FromAccount,
                toAccountNumber = request.BusinessAccountNumber, //@params.accountNumber,
                amount = 0,//request.Amount, TODO: remove the 0
                memo = request.Memo,//"User Wallet Transfer",
                yourTnxRef = request.Reference,

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

                var query = await _httpService.Post($"{@params.Url}/wallet/transfer", content, headers);
                var readResponse = await query.Content.ReadAsStringAsync();
                query.EnsureSuccessStatusCode();
             
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<WalletTransferResult>>(readResponse);
                if (response.StatusCode == 200)
                {

                    return response;
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


        public async Task<WalletResponseData> CreateBusinessUserWallet(string WalletName, string Env, string agrovestoUserId)
        {
            var @params = await RetrieveParams(Env);

            var retrieveBusinessWalletDetail = await _walletRepository.GetSterlingWallet(agrovestoUserId);
            if(retrieveBusinessWalletDetail == null) { throw new Exception($"could not retreive business information"); }
            var model = new
            {
                organizationId = @params.OrganizationId,
                ownerId = retrieveBusinessWalletDetail.OwnerId,
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
                var readResponse = await request.Content.ReadAsStringAsync();
                request.EnsureSuccessStatusCode();
              
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

        public async Task<WalletResponseData> CreateWallet(string WalletName, string Env)
        {
            var @params = await RetrieveParams(Env);

            var currencyId = "fd5e474d-bb42-4db1-ab74-e8d2a01047e9";
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
                var request = await _httpService.Get($"{@params.Url}/wallet/list/{@params.OrganizationId}", headers);
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

        public async Task<CafeOneData> GetUserBalance(string walletId, string env)
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
                var request = await _httpService.Get($"{@params.Url}/wallet/getById/{walletId}", headers);
                request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<CafeOneData>>(readResponse);



                return response.Data ;


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
                var request = await _httpService.Get($"{@params.Url}/wallet/{walletId}", headers);
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


        public async Task<(CafeOneWalletProvider data, string message)> GetUserAccount(string internalAccountId, string env)
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
                var requestUrl = $"{@params.Url}/wallet/getByInternalAccountId/{internalAccountId}";
                var request = await _httpService.Get(requestUrl, headers);
                //request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<CafeOneData>>(readResponse);



                return (response.Data.WalletProviders.FirstOrDefault(), "Successful");


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return (null, ex.Message);
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

        public async Task<CafeOnWalletResponse<WalletResponseData>> UserWalletToAgrovestoTransfer(string reference, string ownerId, decimal amount, string fromAccount, string Env)
        {
            var @params = await RetrieveParams(Env);

            var model = new
            {
                organizationId = @params.OrganizationId,
                ownerId = ownerId,
                fromAccountNumber = fromAccount,
                toAccountNumber = @params.accountNumber, 
                amount = 0,//amount, TODO: remove the 0
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
                var readResponse = await request.Content.ReadAsStringAsync();
                request.EnsureSuccessStatusCode();
               
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<WalletResponseData>>(readResponse);
                if (response.StatusCode == 200)
                {

                    return response;
                }
                else
                {
                    return null;//new WalletResponseData();
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
              
                var response = JsonConvert.DeserializeObject<CafeOnWalletResponse<CafeOneAuthResponse>>(readResponse);



                return response.Data;


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
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

