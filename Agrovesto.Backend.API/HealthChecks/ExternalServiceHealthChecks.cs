using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Agrovesto.Backend.API.HealthChecks
{
    public class IdentityPassHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public IdentityPassHealthCheck(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var baseUrl = _configuration["IdentityPass:preembleyurl"];
                var response = await _httpClient.GetAsync($"{baseUrl}/api/v1/test", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Identity Pass API is accessible");
                }
                else
                {
                    return HealthCheckResult.Degraded($"Identity Pass API returned status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Unhealthy("Identity Pass API is not accessible", ex);
            }
            catch (TaskCanceledException ex)
            {
                return HealthCheckResult.Unhealthy("Identity Pass API request timeout", ex);
            }
        }
    }

    public class TermiiSmsHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public TermiiSmsHealthCheck(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var baseUrl = _configuration["TermiSMS:url"];
                var apiKey = _configuration["TermiSMS:livekey"];
                
                if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
                {
                    return HealthCheckResult.Degraded("Termii SMS configuration is incomplete");
                }

                // Test with a simple balance check endpoint (if available)
                var balanceUrl = "https://v3.api.termii.com/api/get-balance";
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
                
                var response = await _httpClient.GetAsync(balanceUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Termii SMS API is accessible");
                }
                else
                {
                    return HealthCheckResult.Degraded($"Termii SMS API returned status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Unhealthy("Termii SMS API is not accessible", ex);
            }
            catch (TaskCanceledException ex)
            {
                return HealthCheckResult.Unhealthy("Termii SMS API request timeout", ex);
            }
        }
    }

    public class CountryStateCityApiHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public CountryStateCityApiHealthCheck(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
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

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-CSCAPI-KEY", apiKey);
                
                var response = await _httpClient.GetAsync($"{baseUrl}/countries", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Country State City API is accessible");
                }
                else
                {
                    return HealthCheckResult.Degraded($"Country State City API returned status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Unhealthy("Country State City API is not accessible", ex);
            }
            catch (TaskCanceledException ex)
            {
                return HealthCheckResult.Unhealthy("Country State City API request timeout", ex);
            }
        }
    }

    public class SendGridHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SendGridHealthCheck(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
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

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                
                // Check SendGrid API status
                var response = await _httpClient.GetAsync("https://api.sendgrid.com/v3/user/profile", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("SendGrid API is accessible");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return HealthCheckResult.Degraded("SendGrid API key may be invalid");
                }
                else
                {
                    return HealthCheckResult.Degraded($"SendGrid API returned status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Unhealthy("SendGrid API is not accessible", ex);
            }
            catch (TaskCanceledException ex)
            {
                return HealthCheckResult.Unhealthy("SendGrid API request timeout", ex);
            }
        }
    }
}
