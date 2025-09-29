using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml.Interfaces.Net;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{
    public class FCMBWalletService<T,R> : IWalletService<T, FCMBWalletResponse<R>> where T : class
    {

        private readonly IHttpService _httpService;
        private readonly ILogger<FCMBWalletService<T,R>> _log;
        public FCMBWalletService(ILogger<FCMBWalletService<T,R>> log, IHttpService httpService)
        {

            _httpService = httpService;

            _log = log;
        }

        //fetch auth token
        //fetch bank
        //create virtual account
        //name enquiry
        //transfer fund
        //transaction status
        //get all virtual accounts
        //delete account
        //get account details
        private async Task<FCMBParams> RetrieveParams(WalletProviderParameterModel[] parameters, string Env)
        {
            var response = new FCMBParams();
          //  var provider = await _providerRepo.GetActiveProvider();
            //var providerId = provider.Where(x => x.Code == "CafeOne").FirstOrDefault().ProviderId;
           // var parameters = await _providerRepo.GetParametersByProvider(providerId, Env);
            var _params = parameters.ToDictionary(x => x.Key);

            response.GrantType = _params["GrantType"].Value;
            response.ClientId = _params["ClientId"].Value;
            response.ClientSecret = _params["ClientSecret"].Value;
            response.Scope = _params["Scope"].Value;
            response.BaseUrl = _params["BaseUrl"].Value;
            
            return response;

        }
        private async Task<TokenResponse> GetProviderToken(FCMBParams tokendata)
        {
            
            var model = new
            {
                grant_type = tokendata.GrantType,
                client_id = tokendata.ClientId,
                client_secret = tokendata.ClientSecret,
                scope = tokendata.Scope
            };
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                var request = await _httpService.Post($"{tokendata.BaseUrl}/token", content);
                var readResponse = await request.Content.ReadAsStringAsync();
                request.EnsureSuccessStatusCode();

                var response = JsonConvert.DeserializeObject<TokenResponse>(readResponse);
                return response;

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return null;
            }

        }

        public async Task<FCMBWalletResponse<R>> CreateWallet(T model, WalletProviderParameterModel[] parameters, string env)
        {
            var @params = await RetrieveParams(parameters, env);
            
            try
            {
                //var email = model?.GetType().GetProperty("Email")?.GetValue(model)?.ToString();
                //var firstName = model?.GetType().GetProperty("FirstName")?.GetValue(model)?.ToString();
                //var lastName = model?.GetType().GetProperty("LastName")?.GetValue(model)?.ToString();
                //var phone = model?.GetType().GetProperty("PhoneNumber")?.GetValue(model)?.ToString();


                var data = new
                {
                    email = (string)model.GetType().GetProperty("Email")?.GetValue(model),
                    firstName = (string)model.GetType().GetProperty("FirstName")?.GetValue(model),
                    lastName = (string)model.GetType().GetProperty("LastName")?.GetValue(model),
                    phone = (string)model.GetType().GetProperty("PhoneNumber")?.GetValue(model)
                };

                var accessToken = await GetProviderToken(@params);


                var headers = new Dictionary<string, string>
                {
                    { "Authorization",  $"Bearer {accessToken.AccessToken}"}
                };
                _log.LogInformation("Calling enpoint with payload");
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.BaseUrl}/virtual-account/static", content,  accessToken.AccessToken);

                _log.LogInformation($"API response from Create wallet {request}");
                var readResponse = await request.Content.ReadAsStringAsync();

                _log.LogInformation($"reading response from Create wallet {readResponse}");

                var response = JsonConvert.DeserializeObject<FCMBWalletResponse<R>>(readResponse);

                    
                    return response;
                

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                 _log.LogError(ex, $"Error Creating Wallet at FCMBWallet Service: {errorMsg}");
                throw;
            }

        }

        public  async Task<FCMBWalletResponse<R>> GetBanks(T model, WalletProviderParameterModel[] parameters, string env)
        {
            var @params = await RetrieveParams(parameters, env);


            try
            {
                var accessToken = await GetProviderToken(@params);


                var headers = new Dictionary<string, string>
                {
                    { "Authorization",  $"Bearer {accessToken.AccessToken}"}
                };
                
                
                var request = await _httpService.Get($"{@params.BaseUrl}/banks", headers);
                // request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<FCMBWalletResponse<R>>(readResponse);

                return response;



            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                _log.LogError(ex, $"Error retrieving bank lists at FCMBWallet Service: {errorMsg}");

                throw;
            }

        }
        public async Task<FCMBWalletResponse<R>> GetWalletDetails(T model, WalletProviderParameterModel[] parameters, string env)
        {
            var @params = await RetrieveParams(parameters, env);


            try
            {
                var accessToken = await GetProviderToken(@params);


                var headers = new Dictionary<string, string>
                {
                    { "Authorization",  $"Bearer {accessToken.AccessToken}"}
                };

                var accountNumber = (string)model.GetType().GetProperty("AccountNumber")?.GetValue(model);
                var request = await _httpService.Get($"{@params.BaseUrl}/virtual-account/static/{accountNumber}", headers);
                // request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<FCMBWalletResponse<R>>(readResponse);

                return response;



            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                _log.LogError(ex, $"Error retrieving bank lists at FCMBWallet Service: {errorMsg}");

                throw;
            }
        }


        public async Task<FCMBWalletResponse<R>> NameEnquiry(T model, WalletProviderParameterModel[] parameters, string env)
        {
            throw new NotImplementedException();
        }

     
        public  async Task<FCMBWalletResponse<R>> InitiateFundTransfer(T model, WalletProviderParameterModel[] parameters, string env)
        {
            throw new NotImplementedException();
        }

        public async Task<FCMBWalletResponse<R>> FetchTransferStatus(T model, WalletProviderParameterModel[] parameters, string env)
        {
            throw new NotImplementedException();
        }

        public async Task<FCMBWalletResponse<R>> GetAllWallets(T model, WalletProviderParameterModel[] parameters, string env)
        {
            throw new NotImplementedException();
        }

       
    }

}
