using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Agrovesto.Backend.API.HealthChecks
{
    public class IdentityPassHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public IdentityPassHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var baseUrl = _configuration["IdentityPass:preembleyurl"];

                if (string.IsNullOrEmpty(baseUrl))
                {
                    return HealthCheckResult.Degraded("IdentityPass configuration (IdentityPass:preembleyurl) is not configured");
                }

                // Prembly does not expose a /test endpoint. Probe the configured base URL
                // itself with a HEAD/GET — any 2xx/3xx/4xx response proves the host is
                // reachable, while a connection error or DNS failure returns Degraded.
                var client = _httpClientFactory.CreateClient("health");
                using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl);
                var response = await client.SendAsync(request, cancellationToken);

                // Any response from the host means it is reachable. 5xx indicates the host
                // is up but unhealthy; treat that as Degraded.
                if (response.IsSuccessStatusCode || (int)response.StatusCode < 500)
                {
                    return HealthCheckResult.Healthy("Identity Pass API is accessible");
                }

                return HealthCheckResult.Degraded($"Identity Pass API returned status code: {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Degraded("Identity Pass API is not accessible", ex);
            }
            catch (TaskCanceledException ex)
            {
                return HealthCheckResult.Degraded("Identity Pass API request timeout", ex);
            }
        }
    }

    public class TermiiSmsHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public TermiiSmsHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // The configured base URL is optional; if not set we fall back to the
                // canonical Termii v3 endpoint. The only required setting is the API key.
                var baseUrl = _configuration["TermiSMS:url"];
                var apiKey = _configuration["TermiSMS:livekey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    return HealthCheckResult.Degraded("Termii SMS API key (TermiSMS:livekey) is not configured");
                }

                var balanceUrl = string.IsNullOrEmpty(baseUrl)
                    ? "https://v3.api.termii.com/api/get-balance"
                    : $"{baseUrl.TrimEnd('/')}/api/get-balance";

                // Use a dedicated short-timeout client so a hung upstream cannot stall /health.
                using var request = new HttpRequestMessage(HttpMethod.Get, balanceUrl);
                request.Headers.Add("X-API-KEY", apiKey);

                var client = _httpClientFactory.CreateClient("health");
                var response = await client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Termii SMS API is accessible");
                }

                return HealthCheckResult.Degraded($"Termii SMS API returned status code: {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Degraded("Termii SMS API is not accessible", ex);
            }
            catch (TaskCanceledException ex)
            {
                return HealthCheckResult.Degraded("Termii SMS API request timeout", ex);
            }
        }
    }

    public class CountryStateCityApiHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public CountryStateCityApiHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var baseUrl = _configuration["Location:apiUrl"];
                var apiKey = _configuration["Location:apiKey"];

                if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
                {
                    return HealthCheckResult.Degraded("Country State City API configuration is incomplete");
                }

                var client = _httpClientFactory.CreateClient("health");
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/countries");
                request.Headers.Add("X-CSCAPI-KEY", apiKey);

                var response = await client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Country State City API is accessible");
                }

                return HealthCheckResult.Degraded($"Country State City API returned status code: {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Degraded("Country State City API is not accessible", ex);
            }
            catch (TaskCanceledException ex)
            {
                return HealthCheckResult.Degraded("Country State City API request timeout", ex);
            }
        }
    }

    public class SendGridHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SendGridHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var apiKey = _configuration["SendGrid:key"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    return HealthCheckResult.Degraded("SendGrid API key is not configured");
                }

                var client = _httpClientFactory.CreateClient("health");
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.sendgrid.com/v3/user/profile");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                var response = await client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("SendGrid API is accessible");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return HealthCheckResult.Degraded("SendGrid API key may be invalid");
                }

                return HealthCheckResult.Degraded($"SendGrid API returned status code: {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Degraded("SendGrid API is not accessible", ex);
            }
            catch (TaskCanceledException ex)
            {
                return HealthCheckResult.Degraded("SendGrid API request timeout", ex);
            }
        }
    }
}
