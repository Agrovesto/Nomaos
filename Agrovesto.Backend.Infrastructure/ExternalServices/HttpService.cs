using Agrovesto.Backend.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices
{
    public class HttpService : IHttpService
    {
        public async Task<T> Get<T>(string url, string token = null)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            using (HttpClient client = new HttpClient(handler))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                HttpResponseMessage httpResponseMessage = await client.GetAsync(url);
                string response = await httpResponseMessage.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<T>(response);
                client.Dispose();
                return result;
            };
        }
        public async Task<T> Get<T>(string url, IDictionary<string, string> headers, string token = null)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            using (HttpClient client = new HttpClient(handler))
            {
                foreach (var tm in headers)
                {
                    client.DefaultRequestHeaders.Add(tm.Key, tm.Value);
                }
                HttpResponseMessage httpResponseMessage = await client.GetAsync(url);
                string response = await httpResponseMessage.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<T>(response);
                client.Dispose();
                return result;
            };
        }
        public async Task<T> Post<T>(string url, HttpContent content, string token = null)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            using (HttpClient client = new HttpClient(handler))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                HttpResponseMessage httpResponseMessage = await client.PostAsync(url, content);
                string response = await httpResponseMessage.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<T>(response);
                client.Dispose();
                return result;
            };
        }
        public async Task<HttpResponseMessage> Post(string url, HttpContent content, IDictionary<string, string> headers, string token = null)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            using (HttpClient client = new HttpClient(handler, true))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                foreach (var tm in headers)
                {
                    client.DefaultRequestHeaders.Add(tm.Key, tm.Value);
                }
                return await client.PostAsync(url, content);
            };
        }
        public async Task<HttpResponseMessage> Post(string url, HttpContent content, string token = null)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            using (HttpClient client = new HttpClient(handler, true))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                return await client.PostAsync(url, content);
            };
        }

        public async Task<T> Put<T>(string url, HttpContent content, string token = null)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            using (HttpClient client = new HttpClient(handler, true))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                HttpResponseMessage httpResponseMessage = await client.PutAsync(url, content);
                string response = await httpResponseMessage.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<T>(response);
                return result;
            };
        }

        public async Task<HttpResponseMessage> Put(string url, HttpContent content, IDictionary<string, string> headers, string token = null)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            using (HttpClient client = new HttpClient(handler, true))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                foreach (var tm in headers)
                {
                    client.DefaultRequestHeaders.Add(tm.Key, tm.Value);
                }

                return await client.PutAsync(url, content);

            };
        }
        public async Task<T> Delete<T>(string url, string token = null)
        {

            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            using (HttpClient client = new HttpClient(handler, true))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                HttpResponseMessage httpResponseMessage = await client.DeleteAsync(url);
                string response = await httpResponseMessage.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<T>(response);
                return result;
            };
        }
    }

}