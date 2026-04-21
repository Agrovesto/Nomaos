using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.BGService
{
    public class MtnMomoWebhookHandler : IProviderWebhookHandler
    {
        private readonly string _subscriptionKey;

        public MtnMomoWebhookHandler(IConfiguration config)
        {
            _subscriptionKey = config["MTN:SubscriptionKey"];
        }

        public string ProviderName => "MTN";

        public async Task<ProviderWebhookRequest> ParseAsync(HttpRequest request)
        {
            var payload = await new StreamReader(request.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(payload);

            return new ProviderWebhookRequest
            {
                TransactionReference = data?.externalId,
                ProviderReference = data?.financialTransactionId,
                Status = ProviderMapStatus.MapStatus((string)data?.status),
                Amount = data?.amount,
                Currency = data?.currency,
                EventTime = DateTimeOffset.UtcNow
            };
        }

        public Task<bool> ValidateSignatureAsync(HttpRequest request, string rawBody)
        {
            // MTN usually uses subscription key header
            var key = request.Headers["Ocp-Apim-Subscription-Key"];
            return Task.FromResult(key == _subscriptionKey);
        }
    }
}
