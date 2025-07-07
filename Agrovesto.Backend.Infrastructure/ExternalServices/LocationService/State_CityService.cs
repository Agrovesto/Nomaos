using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.S3.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.LocationService
{
    public static class State_CityService
    {

        public static async Task<List<States>> GetStates()
        {
            var url = AppSettingsHelper.Setting("Location:apiUrl");
            var route = AppSettingsHelper.Setting("Location:country_code");
            var key = AppSettingsHelper.Setting("Location:apiKey");
            string baseUrl = $"{url}/countries/{route}/states";

            var result = await GetWithAPIKey<List<States>>(baseUrl);

            return result;
        }
        public static async Task<List<Cities>> GetCities(string stateCiso)
        {
            var url = AppSettingsHelper.Setting("Location:apiUrl");
            var route = AppSettingsHelper.Setting("Location:country_code");
            var key = AppSettingsHelper.Setting("Location:apiKey");
            string baseUrl = $"{url}/countries/{route}/states/{stateCiso}/cities";

            var result = await GetWithAPIKey<List<Cities>>(baseUrl);

            return result;
        }
        public static async Task<T> GetWithAPIKey<T>(string url)
        {
            var key = AppSettingsHelper.Setting("Location:apiKey");
            using (HttpClient client = new HttpClient())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                ////   Console.WriteLine($"URL: {url}");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.ResetColor();

                try
                {
                    string apiKey = key;
                    client.DefaultRequestHeaders.Add("X-CSCAPI-KEY", apiKey);

                    HttpResponseMessage httpResponseMessage = await client.GetAsync(url);
                    string response = await httpResponseMessage.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<T>(response);
                    return result;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    string errMsg = e.InnerException != null ? e.InnerException.Message : e.Message;
                    //   Console.WriteLine($"Error: {errMsg}");
                    //  Console.WriteLine($"Error: {e.StackTrace}");
                    Console.ResetColor();

                    return default(T);
                }
            };
        }
    }
}
