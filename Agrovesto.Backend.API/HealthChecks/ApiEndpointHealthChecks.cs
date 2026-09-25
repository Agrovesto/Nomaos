using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace Agrovesto.Backend.API.HealthChecks
{
    public class ApiEndpointHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiEndpointHealthCheck> _logger;

        public ApiEndpointHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ApiEndpointHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, object>();
            var allHealthy = true;
            var issues = new List<string>();

            // Get base URL for the application
            var baseUrl = GetBaseUrl();

            // Define critical endpoints to check (public endpoints that don't require auth)
            var endpointsToCheck = new[]
            {
                new { Name = "Health Status", Url = $"{baseUrl}/health/status", ExpectedStatus = HttpStatusCode.OK },
                new { Name = "Swagger UI", Url = $"{baseUrl}/swagger", ExpectedStatus = HttpStatusCode.OK },
                new { Name = "API Documentation", Url = $"{baseUrl}/swagger/v1/swagger.json", ExpectedStatus = HttpStatusCode.OK }
            };

            var client = _httpClientFactory.CreateClient("health");

            foreach (var endpoint in endpointsToCheck)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint.Url);
                    request.Headers.Add("User-Agent", "Agrovesto-HealthCheck/1.0");

                    var response = await client.SendAsync(request, cancellationToken);
                    stopwatch.Stop();

                    var endpointResult = new
                    {
                        Status = response.StatusCode.ToString(),
                        IsHealthy = response.StatusCode == endpoint.ExpectedStatus,
                        ResponseTime = $"{stopwatch.ElapsedMilliseconds:F0}ms",
                        StatusCode = (int)response.StatusCode
                    };

                    results[endpoint.Name] = endpointResult;

                    if (response.StatusCode != endpoint.ExpectedStatus)
                    {
                        allHealthy = false;
                        issues.Add($"{endpoint.Name}: Expected {endpoint.ExpectedStatus}, got {response.StatusCode}");
                    }

                    _logger.LogDebug("Endpoint {EndpointName} check: {StatusCode}", endpoint.Name, response.StatusCode);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    allHealthy = false;
                    var errorMessage = $"{endpoint.Name}: {ex.Message}";
                    issues.Add(errorMessage);

                    results[endpoint.Name] = new
                    {
                        Status = "Error",
                        IsHealthy = false,
                        Error = ex.Message,
                        ResponseTime = $"{stopwatch.ElapsedMilliseconds:F0}ms"
                    };

                    _logger.LogWarning(ex, "Endpoint {EndpointName} check failed", endpoint.Name);
                }
            }

            // Check API controller availability (without authentication)
            await CheckControllerAvailability(client, baseUrl, results, issues, cancellationToken);

            var description = allHealthy
                ? "All API endpoints are responding correctly"
                : $"Issues detected: {string.Join(", ", issues)}";

            var status = allHealthy ? HealthStatus.Healthy : HealthStatus.Degraded;

            return new HealthCheckResult(status, description, data: results);
        }

        private async Task CheckControllerAvailability(HttpClient client, string baseUrl, Dictionary<string, object> results, List<string> issues, CancellationToken cancellationToken)
        {
            // Check if main API controllers are accessible (they should return 401 for unauthorized requests, not 404)
            // Use lightweight public ping endpoints to verify controller availability
            var controllerEndpoints = new[]
            {
                new { Name = "Authentication API", Url = $"{baseUrl}/api/v2/account/ping", Method = HttpMethod.Get },
                new { Name = "User API", Url = $"{baseUrl}/api/v2/user/ping", Method = HttpMethod.Get },
                new { Name = "Location API", Url = $"{baseUrl}/api/v2/location/ping", Method = HttpMethod.Get },
                new { Name = "Business API", Url = $"{baseUrl}/api/v2/business/ping", Method = HttpMethod.Get },
                new { Name = "Farm Management API", Url = $"{baseUrl}/api/v2/farm/ping", Method = HttpMethod.Get }
            };

            foreach (var endpoint in controllerEndpoints)
            {
                try
                {
                    using var request = new HttpRequestMessage(endpoint.Method, endpoint.Url);
                    request.Headers.Add("User-Agent", "Agrovesto-HealthCheck/1.0");

                    var response = await client.SendAsync(request, cancellationToken);

                    // For API endpoints, we expect either 200 (if public) or 401 (if auth required)
                    // 404 would indicate the controller/endpoint doesn't exist
                    var isHealthy = response.StatusCode != HttpStatusCode.NotFound &&
                                   response.StatusCode != HttpStatusCode.InternalServerError;

                    results[$"Controller_{endpoint.Name}"] = new
                    {
                        Status = response.StatusCode.ToString(),
                        IsHealthy = isHealthy,
                        StatusCode = (int)response.StatusCode,
                        Note = GetStatusNote(response.StatusCode)
                    };

                    if (!isHealthy)
                    {
                        issues.Add($"{endpoint.Name}: Controller not accessible ({response.StatusCode})");
                    }
                }
                catch (Exception ex)
                {
                    results[$"Controller_{endpoint.Name}"] = new
                    {
                        Status = "Error",
                        IsHealthy = false,
                        Error = ex.Message
                    };
                    issues.Add($"{endpoint.Name}: {ex.Message}");
                }
            }
        }

        private string GetStatusNote(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.OK => "Endpoint accessible",
                HttpStatusCode.Unauthorized => "Endpoint exists (auth required)",
                HttpStatusCode.BadRequest => "Endpoint exists (validation required)",
                HttpStatusCode.NotFound => "Endpoint not found",
                HttpStatusCode.InternalServerError => "Server error",
                _ => "Unexpected response"
            };
        }

        private string GetBaseUrl()
        {
            // Try to get from configuration first
            var configuredUrl = _configuration["BaseUrl"] ?? _configuration["ApplicationUrl"];
            if (!string.IsNullOrEmpty(configuredUrl))
            {
                return configuredUrl.TrimEnd('/');
            }

            // Fall back to ASPNETCORE_URLS or a sensible default.
            var aspnetCoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            if (!string.IsNullOrEmpty(aspnetCoreUrls))
            {
                var first = aspnetCoreUrls.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(first))
                {
                    return first.TrimEnd('/');
                }
            }

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                return "https://localhost:5001";
            }

            // For production, try to determine from server
            return "http://localhost:5000"; // Fallback
        }
    }

    public class ApplicationComponentHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApplicationComponentHealthCheck> _logger;

        public ApplicationComponentHealthCheck(IServiceProvider serviceProvider, ILogger<ApplicationComponentHealthCheck> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, object>();
            var allHealthy = true;
            var issues = new List<string>();

            // Check if key services are registered and can be resolved
            var servicesToCheck = new[]
            {
                typeof(Core.Interfaces.Repositories.IAuthenticationRepository),
                typeof(Core.Interfaces.Managers.IBusinessManager),
                typeof(Core.Interfaces.Managers.IWalletManager),
                typeof(Infrastructure.DB.DataContext)
            };

            foreach (var serviceType in servicesToCheck)
            {
                try
                {
                    var service = _serviceProvider.GetService(serviceType);
                    var serviceName = serviceType.Name;
                    
                    results[serviceName] = new
                    {
                        Status = service != null ? "Available" : "Not Registered",
                        IsHealthy = service != null,
                        Type = serviceType.FullName
                    };

                    if (service == null)
                    {
                        allHealthy = false;
                        issues.Add($"{serviceName} is not registered in DI container");
                    }

                    _logger.LogDebug("Service {ServiceName} check: {Status}", serviceName, service != null ? "Available" : "Missing");
                }
                catch (Exception ex)
                {
                    allHealthy = false;
                    var serviceName = serviceType.Name;
                    issues.Add($"{serviceName}: {ex.Message}");
                    
                    results[serviceName] = new
                    {
                        Status = "Error",
                        IsHealthy = false,
                        Error = ex.Message
                    };

                    _logger.LogWarning(ex, "Service {ServiceName} check failed", serviceName);
                }
            }

            var description = allHealthy 
                ? "All application components are properly registered and available" 
                : $"Component issues: {string.Join(", ", issues)}";

            var status = allHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;

            return new HealthCheckResult(status, description, data: results);
        }
    }
}