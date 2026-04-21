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
    public class PaystackWebhookHandler : IProviderWebhookHandler
    {
        private readonly string _secret;

        public PaystackWebhookHandler(IConfiguration config)
        {
            _secret = config["Paystack:SecretKey"];
        }

        public string ProviderName => "PAYSTACK";

        public async Task<ProviderWebhookRequest> ParseAsync(HttpRequest request)
        {
            var payload = await new StreamReader(request.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(payload);

            return new ProviderWebhookRequest
            {
                TransactionReference = data?.data?.reference,
                ProviderReference = data?.data?.id?.ToString(),
                Status = data?.data?.status == "success" ? "SUCCESS" : "FAILED",
                Amount = data?.data?.amount != null ? (decimal)data.data.amount / 100 : null,
                Currency = data?.data?.currency,
                EventTime = DateTimeOffset.UtcNow
            };
        }

        public Task<bool> ValidateSignatureAsync(HttpRequest request, string rawBody)
        {
            var signature = request.Headers["x-paystack-signature"];

            using var hmac = new System.Security.Cryptography.HMACSHA512(
                Encoding.UTF8.GetBytes(_secret));

            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
            var computed = BitConverter.ToString(hash).Replace("-", "").ToLower();

            return Task.FromResult(computed == signature);
        }
    }
}
