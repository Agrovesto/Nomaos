using AfricasTalkingCS;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _log;

        private IConfiguration _config;
        public NotificationService(IConfiguration config, ILogger<NotificationService> log)
        {

            _config = config;
            _log = log;
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
            var url = $"{_config["TermiSMS:url"]}/api/sms/send";

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


        public async Task SendEmailAsync(string destination,string subject, string message)
        {
            var apikey = _config["TermiSMS:livekey"];
            var from = _config["TermiSMS:from"];
            var channel = _config["TermiSMS:channel"];
            var type = _config["TermiSMS:type"];
            var email_configuration_id = _config["TermiSMS:EmailId"];
            var template_id = _config["TermiSMS:TemplateId"];
            var url =$"{_config["TermiSMS:url"]}/api/templates/send-email";

            //request.api_key = apikey;
            //request.from = from;
            //request.channel = channel;
            //request.type = type;

            var payload = new
            {
                api_key= apikey,
                email = destination,
                subject=subject,
                email_configuration_id= email_configuration_id,
                template_id= template_id,
                variables = new { 
                    message = message 
                }
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

                if (!response.IsSuccessStatusCode)
                {
                    _log.LogError(
                        "Termii EMAIL failed. Status: {Status}, Body: {Body}",
                        response.StatusCode,
                        responseBody
                    );
                    return;
                }

                _log.LogInformation("Email sent to {Destination}", destination);

                // return (result, response.IsSuccessStatusCode);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
                //return (new TermiiResponse(), false);
                _log.LogError($"Failed to send email to {destination}. Error: {ex.Message}");
            }


            }


        public async Task SendSmsAsync(string destination, string message)
        {
            var formattedDestination = FormatPhoneNumberToInternational(destination);

            var apikey = _config["TermiSMS:livekey"];
            var from = _config["TermiSMS:from"];
            var channel = _config["TermiSMS:channel"];
            var type = _config["TermiSMS:type"];
            var url = $"{_config["TermiSMS:url"]}/api/sms/send";


            _log.LogWarning(
    "Termii config check → key:{HasKey}, url:{Url}",
    !string.IsNullOrWhiteSpace(apikey),
    _config["TermiSMS:url"]
);

            var payload = new
            {
                api_key = apikey,
                to = formattedDestination,
                from = from,
                sms = message,
                channel = channel,
                type = type
            };

            var json = JsonConvert.SerializeObject(payload);

            using var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            try
            {
                var response = await client.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _log.LogError(
                        "Termii SMS failed. Status: {Status}, Body: {Body}",
                        response.StatusCode,
                        responseBody
                    );
                    return;
                }

                _log.LogInformation("SMS sent to {Destination}", formattedDestination);

            }
            catch (HttpRequestException ex)
            {
                _log.LogError($"Failed to send SMS to {formattedDestination}. Error: {ex.Message}");
            }
        }
        private string FormatPhoneNumberToInternational(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Phone number is required.");

            // Ensure numeric
            phoneNumber = phoneNumber.Trim();

            if (!phoneNumber.All(char.IsDigit))
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Phone number must contain only digits.");

            if (phoneNumber.Length != 11)
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Phone number must be exactly 11 digits.");

            // Remove leading zero → last 10 digits
            var localNumber = phoneNumber.Substring(1);

            var countryCode = _config["TermiSMS:CountryCode"]; // "+234"

            if (string.IsNullOrWhiteSpace(countryCode))
                throw new BaseException(
                    Core.Enums.ExceptionType.INTERNAL_SERVER_ERROR,
                    "International phone number format is not configured.");

            return $"{countryCode}{localNumber}";
        }

        //public async Task<bool> SendSMSAsync(TermiiSmsRequest request)
        //{
        //    var apikey = _config["TermiSMS:livekey"];
        //    var from = _config["TermiSMS:from"];
        //    var channel = _config["TermiSMS:channel"];
        //    var type = _config["TermiSMS:type"];
        //    var url = _config["TermiSMS:url"];

        //    request.api_key = apikey;
        //    request.from = from;
        //    request.channel = channel;
        //    request.type = type;

        //    var payload = new
        //    {
        //        to = request.to,
        //        from = from,
        //        sms = request.sms,
        //        type = "plain",
        //        api_key = apikey,
        //        channel = "generic"
        //        // Remove media if it's not accepted by the API
        //        // media = new { url = "...", caption = "..." }
        //    };

        //    var json = JsonConvert.SerializeObject(payload);

        //    using var client = new HttpClient();
        //    var content = new StringContent(json, Encoding.UTF8);
        //    content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

        //    try
        //    {
        //        var response = await client.PostAsync(url, content);
        //        var responseBody = await response.Content.ReadAsStringAsync();

        //        var result = JsonConvert.DeserializeObject<TermiiResponse>(responseBody);

        //        Console.WriteLine($"Status Code: {response.StatusCode}");
        //        Console.WriteLine($"Response: {responseBody}");

        //        return (result, response.IsSuccessStatusCode);
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        Console.WriteLine($"Request error: {ex.Message}");
        //        return (new TermiiResponse(), false);
        //    }


        //}

    }
}

