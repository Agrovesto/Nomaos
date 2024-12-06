using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core
{
    public interface IHttpService
    {
        Task<T> Get<T>(string url, string token = null);
        Task<T> Post<T>(string url, HttpContent content, string token = null);
        Task<HttpResponseMessage> Post(string url, HttpContent content, IDictionary<string, string> headers, string token = null);
        Task<T> Put<T>(string url, HttpContent content, string token = null);
        Task<HttpResponseMessage> Put(string url, HttpContent content, IDictionary<string, string> headers, string token = null);
        Task<T> Delete<T>(string url, string token = null);
        Task<T> Get<T>(string url, IDictionary<string, string> headers, string token = null);
        Task<HttpResponseMessage> Post(string url, HttpContent content, string token = null);
    }
}
