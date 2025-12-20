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
    public class FCMBWalletService : IWalletService
    {

        private readonly IHttpService _httpService;
        private readonly ILogger<FCMBWalletService> _log;
        public FCMBWalletService(ILogger<FCMBWalletService> log, IHttpService httpService)
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
            response.Scope = _params["scope"].Value;
            response.BaseUrl = _params["BaseUrl"].Value;
            
            return response;

        }
        private async Task<TokenResponse> GetProviderToken(FCMBParams tokendata)
        {

            var model = new Dictionary<string, string>
{
    { "grant_type", tokendata.GrantType },
    { "client_id", tokendata.ClientId },
    { "client_secret", tokendata.ClientSecret },
    { "scope", tokendata.Scope }
};

            try
            {
                using var content = new FormUrlEncodedContent(model);
                var request = await _httpService.Post($"{tokendata.BaseUrl}/token", content);

                request.EnsureSuccessStatusCode();

                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<TokenResponse>(readResponse);

                return response;
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException?.Message ?? ex.Message;
                // optionally log errorMsg here
                return null;
            }

        }

        public async Task<WalletsResponse<CreateWalletResponse>> CreateWallet(WalletsRequest model, WalletProviderParameterModel[] parameters, string env)
        {
            var @params = await RetrieveParams(parameters, env);
            
            try
            {
                //var email = model?.GetType().GetProperty("Email")?.GetValue(model)?.ToString();
                //var firstName = model?.GetType().GetProperty("FirstName")?.GetValue(model)?.ToString();
                //var lastName = model?.GetType().GetProperty("LastName")?.GetValue(model)?.ToString();
                //var phone = model?.GetType().GetProperty("PhoneNumber")?.GetValue(model)?.ToString();

                // Default assignments
                var firstName = model.FirstName;
                var lastName = model.LastName;

                // Handle case where FirstName contains multiple words
                if (!string.IsNullOrWhiteSpace(model.FirstName) && model.FirstName.Contains(" "))
                {
                    var nameParts = model.FirstName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (nameParts.Length > 1)
                    {
                        firstName = nameParts.First();   // first word
                        lastName = string.IsNullOrWhiteSpace(model.LastName)
                            ? nameParts.Last()           // use last word if LastName not already provided
                            : model.LastName;
                    }
                }

                var data = new
                {
                    email = model.Email,
                    firstName,
                    lastName,
                    phone = model.PhoneNumber
                };

                var accessToken = await GetProviderToken(@params);


                var headers = new Dictionary<string, string>
                {
                    { "Authorization",  $"Bearer {accessToken.access_token}"}
                };
                _log.LogInformation("Calling enpoint with payload");
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                var request = await _httpService.Post($"{@params.BaseUrl}/virtual-account/static", content,  accessToken.access_token);

                _log.LogInformation($"API response from Create wallet {request}");
                var readResponse = await request.Content.ReadAsStringAsync();

                _log.LogInformation($"reading response from Create wallet {readResponse}");

                var response = JsonConvert.DeserializeObject<WalletsResponse<CreateWalletResponse>>(readResponse);

                    
                    return response;
                

            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                 _log.LogError(ex, $"Error Creating Wallet at FCMBWallet Service: {errorMsg}");
                throw;
            }

        }

        public  async Task<BanksResponseData> GetBanks(WalletsRequest model, WalletProviderParameterModel[] parameters, string env)
        {
            var @params = await RetrieveParams(parameters, env);


            try
            {
                var accessToken = await GetProviderToken(@params);


                var headers = new Dictionary<string, string>
                {
                    { "Authorization",  $"Bearer {accessToken.access_token}"}
                };
                
                
                var request = await _httpService.Get($"{@params.BaseUrl}/banks", headers);
                // request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<WalletsResponse<BanksResponseData>>(readResponse);

                return response.data;



            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                _log.LogError(ex, $"Error retrieving bank lists at FCMBWallet Service: {errorMsg}");

                throw;
            }

        }
        public async Task<CreateWalletResponse> GetWalletDetails(WalletsRequest model, WalletProviderParameterModel[] parameters, string env)
        {
            var @params = await RetrieveParams(parameters, env);


            try
            {
                var accessToken = await GetProviderToken(@params);


                var headers = new Dictionary<string, string>
                {
                    { "Authorization",  $"Bearer {accessToken.access_token}"}
                };

                var accountNumber = (string)model.GetType().GetProperty("AccountNumber")?.GetValue(model);
                var request = await _httpService.Get($"{@params.BaseUrl}/virtual-account/static/{accountNumber}", headers);
                // request.EnsureSuccessStatusCode();
                var readResponse = await request.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<WalletsResponse<CreateWalletResponse>>(readResponse);

                return response.data;



            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                _log.LogError(ex, $"Error retrieving bank lists at FCMBWallet Service: {errorMsg}");

                throw;
            }
        }


        public async Task<CreateWalletResponse> NameEnquiry(WalletsRequest model, WalletProviderParameterModel[] parameters, string env)
        {
            throw new NotImplementedException();
        }

     
        public  async Task<CreateWalletResponse> InitiateFundTransfer(WalletsRequest model, WalletProviderParameterModel[] parameters, string env)
        {
            throw new NotImplementedException();
        }

        public async Task<CreateWalletResponse> FetchTransferStatus(WalletsRequest model, WalletProviderParameterModel[] parameters, string env)
        {
            throw new NotImplementedException();
        }

        public async Task<CreateWalletResponse> GetAllWallets(WalletsRequest model, WalletProviderParameterModel[] parameters, string env)
        {
            throw new NotImplementedException();
        }

       
    }

}
