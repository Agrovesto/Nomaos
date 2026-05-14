using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agrovesto.Backend.Core.Interfaces.Services;
using static Agrovesto.Backend.Core.CoreUtils.Constants;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Exceptions;
using Microsoft.Extensions.Options;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class IdentityVerificationManager : IIdentityVerificationManager
    {

        private readonly IHttpService _httpService;
        private readonly IdentitypassOptions _idPassOption;
        private readonly IEncryptionService _encryptionService;
        public IdentityVerificationManager(IHttpService httpService,
            IOptions<IdentitypassOptions> idPassOption, IEncryptionService encryptionService)
        {
            _httpService = httpService;
            _idPassOption = idPassOption.Value;
            _encryptionService = encryptionService;
        }
    
        public async Task<BVNVerificationResponse<BVN_data, Verification>> VerifyBvn(BVNDataModel model)
        {
            // HttpResponseMessage sendRequest = await PostToEndpoint(new { number=model.number},$"{_idPassOption.Url}/api/v2/biometrics/merchant/data/verification/bvn");
            HttpResponseMessage sendRequest = await PostToEndpoint(new { number = model.number }, $"{_idPassOption.Preembleyurl}/verification/bvn");
            if (sendRequest.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new BVNVerificationResponse<BVN_data, Verification> { data = null, response_code = ResponseCodes.NotFound, detail = $"{model.number} not found", status = false, verification = null };

            }
            var response = await sendRequest.Content.ReadAsStringAsync();
            var bvnInformation = JsonConvert.DeserializeObject<BVNVerificationResponse<BVN_data, Verification>>(response);
            if (bvnInformation.status == false)
            {
                return new BVNVerificationResponse<BVN_data, Verification> { data = bvnInformation.data, response_code = bvnInformation.response_code, detail = bvnInformation.detail, status = bvnInformation.status, verification = bvnInformation.verification };

            }
            return bvnInformation;
        }
        private string HandleIdentityPassException(string responseCode)
        {
            var ninInformation = JsonConvert.DeserializeObject<GlobalResponse<NINData, Verification>>(responseCode);

            string message = "";

            switch (ninInformation.response_code)
            {
                case "00":
                    message = "Successful";
                    break;
                case "01":
                    message = "Record not found";
                    break;
                case "02":
                    message = "Service not available";
                    break;
                case "03":
                    message = "Could not verify NIN at this time";
                    //message = "Insufficient wallet balance";
                    break;
                default:
                    break;
            }
            return message;
        }


        public async Task<NINDataVerificationResponse<NINData, Verification>> VerifyNIN(NINDataModel model)
        {
            var ninInformation = new NINDataVerificationResponse<NINData, Verification>();
            try
            {

                var sendRequest = await PostToEndpoint(new { number_nin = model.number }, $"{_idPassOption.Preembleyurl}/verification/vnin");
                var response = await sendRequest.Content.ReadAsStringAsync();
                var errorCheck = HandleIdentityPassException(response);

                if (sendRequest.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new NINDataVerificationResponse<NINData, Verification>
                    {
                        nin_data = null,
                        response_code = ResponseCodes.NotFound,
                        detail = $"{model.number} not found",
                        status = false,
                        verification = null
                    };
                }

                ninInformation = JsonConvert.DeserializeObject<NINDataVerificationResponse<NINData, Verification>>(response);

                if (!ninInformation.status)
                {
                    return new NINDataVerificationResponse<NINData, Verification>
                    {
                        nin_data = ninInformation.nin_data,
                        response_code = ninInformation.response_code,
                        detail = errorCheck
                        ,//ninInformation.detail,
                        status = ninInformation.status,
                        verification = ninInformation.verification
                    };
                }


                return ninInformation;
            }
            catch (HttpRequestException e)
            {
              //  _log.LogError(e, $"NIN Verification httprequest exception: {e.Message} , {e.StatusCode}");

                throw new NotSuccessfulException($"{e.Message}, {e.StatusCode}");
            }
            catch (Exception e)
            {

               // _log.LogError(e, $"NIN Verification exception: {e.Message}");

                throw new NotSuccessfulException($"Invalid data received, {e.Message}");
            }


        }



        private async Task<HttpResponseMessage> PostToEndpoint(object payload, string url)
        {

            HttpContent content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
         
            IDictionary<string, string> headers = new Dictionary<string, string>
            {
                {"x-api-key",_idPassOption.preembleySecretKey  },
                {"app-id",_idPassOption.preembleyAppId }
            };

            Log.Information($"REquest :{JsonConvert.SerializeObject(payload)}");
            HttpResponseMessage httpResponse = await _httpService.Post($"{url}", content, headers);


            Log.Information($"REsponse:{httpResponse}");
            return httpResponse;
        }

    }
}
