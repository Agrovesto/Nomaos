using AfricasTalkingCS;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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

        public async Task<(SMSResponseModel response, bool status)> HandleAfricasTalkingSMS(WhisperSMSModel request)
        {
            var apikey = _config["AfricasTalking:apikey"];
            var url = _config["AfricasTalking:url"];
            var username = _config["AfricasTalking:username"];

            var gateway = new AfricasTalkingGateway(username, apikey);
            try
            {
                var sms = gateway.SendMessage(request.contacts.FirstOrDefault(), request.message);
                foreach (var res in sms["SMSMessageData"]["Recipients"])
                {
                    Console.WriteLine((string)res["number"] + ": ");
                    Console.WriteLine((string)res["status"] + ": ");
                    Console.WriteLine((string)res["messageId"] + ": ");
                    Console.WriteLine((string)res["cost"] + ": ");
                }

                return (null, true);
            }
            catch (AfricasTalkingGatewayException exception)
            {
                Console.WriteLine(exception);

                return (null, true);
            }

        }

        public async Task<(SMSResponseModel response, bool status)> HandleWhisperSMS(WhisperSMSModel request)
        {
            var apikey = _config["WhisperSMS:apikey"];
            var token = _config["WhisperSMS:token"];
            var url = _config["WhisperSMS:url"];
            var sender_id = _config["WhisperSMS:sender_id"];


            request.sender_id = sender_id;
            request.send_date = DateTime.Now.ToString("dd-MM-yyyy HH:ss");
            var client = new HttpClient();
            SMSResponseModel response;
            // client.DefaultRequestHeaders.Add("Authorization", $"Api_key {token}");
            client.DefaultRequestHeaders.Add("Authorization", token);
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

            var payload = new
            {
                to = request.to,
                from = from,
                sms = request.sms,
                type = "plain",
                api_key = apikey,
                channel = "generic"
                // Remove media if it's not accepted by the API
                // media = new { url = "...", caption = "..." }
            };

            var json = JsonConvert.SerializeObject(payload);

            using var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            try
            {
                var response = await client.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<TermiiResponse>(responseBody);

                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Response: {responseBody}");

                return (result, response.IsSuccessStatusCode);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
                return (new TermiiResponse(), false);
            }


        }
    }
}

