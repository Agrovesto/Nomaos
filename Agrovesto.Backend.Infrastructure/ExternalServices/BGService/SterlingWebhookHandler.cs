using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.BGService
{
    public class SterlingWebhookHandler : IProviderWebhookHandler
    {
        public string ProviderName => "STERLING";

        public async Task<ProviderWebhookRequest> ParseAsync(HttpRequest request)
        {
            var payload = await new StreamReader(request.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(payload);

            return new ProviderWebhookRequest
            {
                TransactionReference = data?.paymentReference,
                ProviderReference = data?.transactionId,
                Status = ProviderMapStatus.MapStatus((string)data?.status),
                Amount = data?.amount,
                Currency = data?.currency,
                EventTime = DateTimeOffset.UtcNow
            };
        }

        public Task<bool> ValidateSignatureAsync(HttpRequest request, string rawBody)
        {
            // If no signature provided, fallback to IP whitelist later
            return Task.FromResult(true);
        }
    }
}
