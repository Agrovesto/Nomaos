using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Core.Models.Payment;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using CafeOneParams = Agrovesto.Backend.Core.Models.Common.CafeOneParams;
using CafeOneBankData = Agrovesto.Backend.Core.Models.Common.CafeOneBankData;
using CafeoneDataModel = Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.CafeoneDataModel;
using CafeoneCustomerRequest = Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.CafeoneCustomerRequest;
using CafeOneAuthResponse = Agrovesto.Backend.Core.Models.Common.CafeOneAuthResponse;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{

    public class CafeOneWalletService : ICafeOneWalletService
    {
        private readonly IWalletProviderRepository _providerRepo;
        private readonly IHttpService _httpService;
        private readonly IWalletRepository _walletRepository;
        private readonly ILogger<CafeOneWalletService> _log;
        public CafeOneWalletService(ILogger<CafeOneWalletService> log, IHttpService httpService, IWalletProviderRepository providerRepo, IWalletRepository walletRepository)
        {
            _providerRepo = providerRepo;
            _httpService = httpService;
            _walletRepository = walletRepository;

            _log = log;
        }
        public async Task<CafeOneParams> RetrieveParams(string Env)
        {
            var response = new CafeOneParams();
            var provider = await _providerRepo.GetActiveProvider();
            var providerId = provider.Where(x => x.Code == "CafeOne").FirstOrDefault().ProviderId;
            WalletProviderParameterModel[] parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
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


        #region Customer Account Creation

        public async Task<NameEnquiry> InterBankNameEnquiry(string accountNumber, string bankCode, string env)
        {
            var @params = await RetrieveParams(env);


            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);

                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                    //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var payload = new
                {
                    bankCode = bankCode,
                    accountNumber = accountNumber
                };
                _log.LogInformation($"Interbank Name Enquiry. Request URL: {@params.payoutUrl}");

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                _log.LogInformation($"Request Payload: {content}");


                var request = await _httpService.Post($"{@params.payoutUrl}/api/Payout/name-enquiry", content,headers);
                // request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                _log.LogInformation($"Response received for Name Enquiry. Response: {readResponse}");

                var response = JsonConvert.DeserializeObject<NameEnquiry>(readResponse);



                return response;


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _log.LogError($"Name Enquiry failed. Error: {errorMsg}");

                return null;
            }

        }

        public async Task<NameEnquiry> IntraBankNameEnquiry(string accountNumber,string env)
        {
            var @params = await RetrieveParams(env);


            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                    //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var request = await _httpService.Get($"{@params.payoutUrl}/api/Payout/intra-bank-enquiry/{accountNumber}", headers);
                // request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<NameEnquiry>(readResponse);



                return response;


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
                  //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var request = await _httpService.Get($"{@params.payoutUrl}/api/Payout/banks", headers);
                // request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<Core.Models.Common.AccountResolutionResponse<List<CafeOneBankData>>>(readResponse);



                return response.data;


            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
            }

        }

        public async Task<List<CafeoneDataModel>> GetCustomers(string env)
        {
            var @params = await RetrieveParams(env);


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

        //Step 1
        public async Task<CafeoneDataModel> CreateCustomer(CafeoneCustomerRequest model, string env)
        {
            try
            {
                var @params = await RetrieveParams(env);
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
                    return await HandleDuplicateCustomer(model, @params, response, env);
                }

                if (response.success)
                {
                    await VerifyKYC(model, @params, response, env);
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
            string env)
        {
            var customers = await GetCustomers(env);
            var matchedCustomer = customers.FirstOrDefault(x => x.MobileNumber == model.mobileNumber);
            if (matchedCustomer != null)
            {
                response.data = matchedCustomer;
                await VerifyKYC(model, @params, response, env);

           
                return response.data;
            }

            return null;
        }


        //public async Task<CafeoneDataModel> CreateCustomer(CafeoneCustomerRequest model, string Env)
        //{
        //    var @params = await RetrieveParams(Env);


        //    try
        //    {
        //        model.CountryId = @params.countryId;
        //        model.CustomerTypeId = @params.customerTypeId;

        //        var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


        //        var headers = new Dictionary<string, string>
        //        {
        //            { "x-api-key",  $"{@params.SecretKey}"},
        //           // { "Authorization",  $"Bearer {token.AccessToken}"}
        //        };
        //        var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

        //        var request = await _httpService.Post($"{@params.Url}/api/v1/customers/add", content, headers);
        //        var readResponse = await request.Content.ReadAsStringAsync();
        //        //request.EnsureSuccessStatusCode();

        //        var response = JsonConvert.DeserializeObject<CafeoneResponseModel<CafeoneDataModel>>(readResponse);
        //        if(response.message.Contains("A customer "))
        //        {
        //            var getCustomers = await GetCustomers(Env);
        //            if(getCustomers.Count > 0)
        //            {
        //                var filterForPhone = getCustomers.FirstOrDefault(x => x.MobileNumber == model.mobileNumber);
        //                if(filterForPhone != null)
        //                {
        //                    await VerifyKYC(model, @params, response, Env);
        //                    return response.data;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            if (response.success)
        //            {
        //                //Step 2
        //                await VerifyKYC(model, @params, response, Env);
        //                return response.data;
        //            }
        //            else
        //            {
        //                return null;
        //            }

        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        //        //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
        //        return null;
        //    }
        //}

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

                var request = await _httpService.Get($"{@params.Url}/api/v1/wallets/get/customer/{customerId}/wallet/{walletId}",  headers);
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

        public async Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> PayoutTransferFromUserAccountToAgrovestoAccount(PostWalletToWalletTransferRequestModel request, string Env)
        {
            var @params = await RetrieveParams(Env);


            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);

               

                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                  //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                request.destinationAccountNumber = @params.accountNumber;
                var nipNameEnquiry = await IntraBankNameEnquiry(request.destinationAccountNumber, Env);
                if(nipNameEnquiry != null)
                {
                    request.destinationAccountNumber = nipNameEnquiry.data.accountNumber;
                    request.destinationAccountName = nipNameEnquiry.data.accountName;
                    request.destinationBankCode = !string.IsNullOrEmpty(nipNameEnquiry.data.destinationBankCode) ? nipNameEnquiry.data.destinationBankCode : "000001";
                    
                }
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                var query = await _httpService.Post($"{@params.payoutUrl}/api/Payout/intrabank-funds-transfer", content, headers);
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

        public async Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> PayoutTransferFromUserAccountToBusinessAccount(PostWalletToWalletTransferRequestModel request, string Env)
        {
            var @params = await RetrieveParams(Env);


            try
            {

                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                  //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var nipNameEnquiry = await IntraBankNameEnquiry(request.destinationAccountNumber, Env);
                var payload = new
                {
                    fromAccount = request.sourceAccountNumber,
                    toAccount = request.destinationAccountNumber,
                    amount = request.Amount,
                    transactionReference = request.reference,
                    remarks = request.remarks,
                    transactionTypeId = 0
                };
                //if (nipNameEnquiry != null)
                //{
                //    request.destinationAccountNumber = nipNameEnquiry.data.accountNumber;
                //    request.destinationAccountName = nipNameEnquiry.data.accountName;
                //    request.destinationBankCode = !string.IsNullOrEmpty(nipNameEnquiry.data.destinationBankCode) ? nipNameEnquiry.data.destinationBankCode : "000001";
                //}
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                //var query = await _httpService.Post($"{@params.payoutUrl}/api/Payout/intrabank-funds-transfer", content, headers);
                var query = await _httpService.Put($"{@params.Url}/api/v1/wallets/wallet/transaction/v2/wallet-to-wallet", content, headers);
                var readResponse = await query.Content.ReadAsStringAsync();
                //query.EnsureSuccessStatusCode();

                var response = JsonConvert.DeserializeObject<CafeoneResponseModel<CafeOneCustomerWalletResponse>>(readResponse);
                return response;

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                //  _logService.Error(ex, $"Flutterwave Initiate Payment Api Error: {errorMsg}");
                return null;
            }
        }

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
                    { "x-api-key",  $"{@params.SecretKey}"},
                  //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var query = await _httpService.Put($"{@params.Url}/api/v1/wallets/wallet/transaction/account-number/post", content, headers);
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
                productId = @params.productDebit,
                organizationId = @params.OrganizationId,                
                amount = request.Amount,
                transactionReference = request.TransactionReference,
                walletId = request.WalletId,
                customerId = request.CustomerId,
                direction = @params.debitdirection,
                remarks = request.Remarks,
                currency = @params.currency,
            
              
                
              
                batchReference = request.BatchReference,
                accountNumber = request.AccountNumber

            };

            try
            {
                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                  //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var query = await _httpService.Put($"{@params.Url}/api/v1/wallets/wallet/transaction/account-number/post", content, headers);
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

        public async Task<CafeoneResponseModel<string>> FundWithdrawal(FundWithdrawalRequestModel request, string Env)
        {
            var @params = await RetrieveParams(Env);


            try
            {

                var token = await GetProviderToken(@params.Url, @params.Email, @params.Password, @params.SecretKey);


                var headers = new Dictionary<string, string>
                {
                    { "x-api-key",  $"{@params.SecretKey}"},
                  //  { "Authorization",  $"Bearer {token.AccessToken}"}
                };
                var nipNameEnquiry = await InterBankNameEnquiry(request.destinationAccountNumber, request.destinationBankCode, Env);
                var source = await InterBankNameEnquiry(request.sourceAccountNumber, "000001", Env);
                if (nipNameEnquiry != null)
                {
                    request.destinationAccountNumber = nipNameEnquiry.data.accountNumber;
                    request.destinationAccountName = nipNameEnquiry.data.accountName;
                    request.destinationBankCode = nipNameEnquiry.data.destinationInstitutionCode; //: "000001";
                    
                }
                request.sourceAccountName = source.data.accountName;
                request.currencyId = @params.currencyId;

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                var query = await _httpService.Post($"{@params.payoutUrl}/api/Payout/inter-bank-transfer", content, headers);
                var readResponse = await query.Content.ReadAsStringAsync();
                query.EnsureSuccessStatusCode();

                var response = JsonConvert.DeserializeObject<CafeoneResponseModel<string>>(readResponse);
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
       

      
        //get wallet   .....https://dev-api.c-one.ng/wallet/list/{ownerId}
        //get wallet by id   .....https://dev-api.c-one.ng/wallet/getById/{walletId}

        //wallet transfer   ....https://dev-api.c-one.ng/wallet/transfer

        // wallet to my bank account transfer ....https://dev-api.c-one.ng/wallet-providers/core/external/transfer
    }
}

