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

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class IdentityVerificationManager : IIdentityVerificationManager
    {

        private readonly IHttpService _httpService;
        private readonly IdentitypassOptions _idPassOption;
        private readonly IEncryptionService _encryptionService;
        public IdentityVerificationManager(IHttpService httpService, IdentitypassOptions idPassOption, IEncryptionService encryptionService)
        {
            _httpService = httpService;
            _idPassOption = idPassOption;
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
