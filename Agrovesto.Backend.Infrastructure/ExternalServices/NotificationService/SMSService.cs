using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService
{
    public class SMSService : ISMSService
    {

        private IConfiguration _config;
        public SMSService(IConfiguration config)
        {

            _config = config;

        }

        public async Task<(SMSResponseModel response, bool status)> HandleWhisperSMS(WhisperSMSModel request)
        {
            var token = _config["WhisperSMS:apikey"];
            var url = _config["WhisperSMS:url"];
            var sender_id = _config["WhisperSMS:sender_id"];


            request.sender_id = sender_id;
            request.send_date = DateTime.Now.ToString("dd-MM-yyyy HH:ss");
            var client = new HttpClient();
            SMSResponseModel response;
            client.DefaultRequestHeaders.Add("Authorization", $"Api_key {token}");
            var data = JsonConvert.SerializeObject(request);

            var sendRequest = await client.PostAsync(url, new StringContent(data, Encoding.UTF8, "application/json"));
            var _response = await sendRequest.Content.ReadAsStringAsync();

            response = JsonConvert.DeserializeObject<SMSResponseModel>(_response);
            return (response, true);
        }


        public async Task<(TermiiResponse response, bool status)> HandleTermiiSMS(TermiiSmsRequest request)
        {
            var apikey = _config["TermiSMS:livekey"];
            var from = _config["TermiSMS:from"];
            var channel = _config["TermiSMS:channel"];
            var type = _config["TermiSMS:type"];
            var url = _config["TermiSMS:url"];
            request.api_key = apikey;
            request.from = from;
            request.channel = channel;
            request.type = type;


            var client = new HttpClient();

            //client.DefaultRequestHeaders.Add("Authorization", $"Api_key {token}");
            var data = JsonConvert.SerializeObject(request);

            try
            {
                var response = await client.PostAsync(url, new StringContent(data, Encoding.UTF8, "application/json"));
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseString);
                var result = JsonConvert.DeserializeObject<TermiiResponse>(responseString);
                return (result, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (new TermiiResponse(), false);
            }
        }

    }
}

