using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.BGService
{
    public interface IProviderWebhookHandler
    {
        string ProviderName { get; }

        Task<ProviderWebhookRequest> ParseAsync(HttpRequest request);

        Task<bool> ValidateSignatureAsync(HttpRequest request, string rawBody);
    }
    public interface IWebhookRouter
    {
        Task RouteAsync(string provider, HttpRequest request);
    }
}
