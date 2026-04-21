using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.BGService
{
    public class WebhookRouter : IWebhookRouter
    {
        private readonly IEnumerable<IProviderWebhookHandler> _handlers;
        private readonly INomaOSWalletService _nomaOSWalletService;

        public WebhookRouter(
            IEnumerable<IProviderWebhookHandler> handlers,
            INomaOSWalletService nomaOSWalletService)
        {
            _handlers = handlers;
            _nomaOSWalletService = nomaOSWalletService;
        }

        public async Task RouteAsync(string provider, HttpRequest request)
        {
            var handler = _handlers.FirstOrDefault(x =>
                x.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

            if (handler == null)
                throw new Exception($"No webhook handler for provider: {provider}");

            // Read raw body ONCE
            request.EnableBuffering();
            var rawBody = await new StreamReader(request.Body).ReadToEndAsync();
            request.Body.Position = 0;

            // Validate signature
            var isValid = await handler.ValidateSignatureAsync(request, rawBody);
            if (!isValid)
                throw new UnauthorizedAccessException("Invalid webhook signature");

            // Parse to normalized model
            var webhookRequest = await handler.ParseAsync(request);

            // Inject raw payload
            webhookRequest.RawPayload = rawBody;
            webhookRequest.Provider = provider;

            // Route to wallet logic
            await _nomaOSWalletService.HandleTopUpWebhook(webhookRequest);
        }
    }
}
