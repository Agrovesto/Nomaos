using Agrovesto.Backend.Core.Models.Businesses.Wallets;
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
    public class FlutterwaveWebhookHandler : IProviderWebhookHandler
    {
        private readonly string _secret;

        public FlutterwaveWebhookHandler(IConfiguration config)
        {
            _secret = config["Flutterwave:SecretHash"];
        }

        public string ProviderName => "FLUTTERWAVE";

        public async Task<ProviderWebhookRequest> ParseAsync(HttpRequest request)
        {
            var payload = await new StreamReader(request.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(payload);

            return new ProviderWebhookRequest
            {
                TransactionReference = data?.data?.tx_ref,
                ProviderReference = data?.data?.id?.ToString(),
                Status = data?.data?.status == "successful" ? "SUCCESS" : "FAILED",
                Amount = data?.data?.amount,
                Currency = data?.data?.currency,
                EventTime = DateTimeOffset.UtcNow
            };
        }

        public Task<bool> ValidateSignatureAsync(HttpRequest request, string rawBody)
        {
            var signature = request.Headers["verif-hash"];
            return Task.FromResult(signature == _secret);
        }
    }
}
