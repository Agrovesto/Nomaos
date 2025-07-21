using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;

namespace Agrovesto.Backend.API.Controllers
{
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            HealthCheckService healthCheckService, 
            ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        /// <summary>
        /// Basic health check - accessible at /health
        /// </summary>
        [HttpGet("/health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                var result = await _healthCheckService.CheckHealthAsync();
                
                var response = new
                {
                    status = result.Status.ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    application = "Agrovesto Backend API",
                    version = GetApplicationVersion(),
                    uptime = GetUptime(),
                    checks = result.Entries.Select(e => new
                    {
                        service = e.Key,
                        status = e.Value.Status.ToString(),
                        responseTime = $"{e.Value.Duration.TotalMilliseconds:F0}ms"
                    }).ToArray()
                };

                var statusCode = result.Status == HealthStatus.Healthy ? 200 : 503;
                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed with exception");
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    error = "Health check system error"
                });
            }
        }

        /// <summary>
        /// Detailed health check - accessible at /health/detailed
        /// </summary>
        [HttpGet("/health/detailed")]
        public async Task<IActionResult> DetailedHealth()
        {
            try
            {
                var result = await _healthCheckService.CheckHealthAsync();
                var process = Process.GetCurrentProcess();
                
                var response = new
                {
                    status = result.Status.ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    application = new
                    {
                        name = "Agrovesto Backend API",
                        version = GetApplicationVersion(),
                        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                        uptime = GetUptime()
                    },
                    summary = new
                    {
                        totalChecks = result.Entries.Count,
                        healthy = result.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                        degraded = result.Entries.Count(e => e.Value.Status == HealthStatus.Degraded),
                        unhealthy = result.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy),
                        totalDuration = $"{result.TotalDuration.TotalMilliseconds:F0}ms"
                    },
                    services = result.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        responseTime = $"{entry.Value.Duration.TotalMilliseconds:F0}ms",
                        description = entry.Value.Description,
                        error = entry.Value.Exception?.Message
                    }).ToArray(),
                    system = new
                    {
                        server = Environment.MachineName,
                        processId = Environment.ProcessId,
                        memoryUsage = $"{Environment.WorkingSet / 1024 / 1024} MB",
                        cpuTime = $"{process.TotalProcessorTime.TotalMilliseconds:F0}ms"
                    }
                };

                var statusCode = result.Status == HealthStatus.Healthy ? 200 : 503;
                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed health check failed with exception");
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    error = "Health check system error",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Quick status check - accessible at /health/status
        /// </summary>
        [HttpGet("/health/status")]
        public IActionResult QuickStatus()
        {
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                application = "Agrovesto Backend API",
                version = GetApplicationVersion(),
                uptime = GetUptime(),
                message = "API is running"
            });
        }

        /// <summary>
        /// Database health check - accessible at /health/database
        /// </summary>
        [HttpGet("/health/database")]
        public async Task<IActionResult> DatabaseHealth()
        {
            try
            {
                var result = await _healthCheckService.CheckHealthAsync(check => 
                    check.Name.Contains("Database") || check.Name.Contains("SQL"));
                
                var dbChecks = result.Entries.Where(e => 
                    e.Key.Contains("Database") || e.Key.Contains("SQL")).ToArray();
                
                var response = new
                {
                    status = result.Status.ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    databases = dbChecks.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        responseTime = $"{e.Value.Duration.TotalMilliseconds:F0}ms",
                        description = e.Value.Description
                    }).ToArray()
                };

                var statusCode = result.Status == HealthStatus.Healthy ? 200 : 503;
                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    error = "Database health check failed"
                });
            }
        }

        /// <summary>
        /// External services health check - accessible at /health/external
        /// </summary>
        [HttpGet("/health/external")]
        public async Task<IActionResult> ExternalServicesHealth()
        {
            try
            {
                var result = await _healthCheckService.CheckHealthAsync(check => 
                    check.Tags.Contains("external"));
                
                var response = new
                {
                    status = "Checked", // Always return "Checked" to avoid cascading failures
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    externalServices = result.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        available = e.Value.Status == HealthStatus.Healthy,
                        responseTime = $"{e.Value.Duration.TotalMilliseconds:F0}ms",
                        description = e.Value.Description
                    }).ToArray(),
                    summary = new
                    {
                        total = result.Entries.Count,
                        available = result.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                        unavailable = result.Entries.Count(e => e.Value.Status != HealthStatus.Healthy)
                    }
                };

                return Ok(response); // Always return 200 OK for external services
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External services health check failed");
                return Ok(new
                {
                    status = "Error",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    error = "External services health check failed"
                });
            }
        }

        /// <summary>
        /// API endpoints health check - accessible at /health/endpoints
        /// </summary>
        [HttpGet("/health/endpoints")]
        public async Task<IActionResult> ApiEndpointsHealth()
        {
            try
            {
                var result = await _healthCheckService.CheckHealthAsync(check => 
                    check.Tags.Contains("api") || check.Tags.Contains("endpoints"));
                
                var response = new
                {
                    status = result.Status.ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    apiEndpoints = result.Entries.SelectMany(e => 
                        e.Value.Data?.Select(d => new
                        {
                            endpoint = d.Key,
                            details = d.Value
                        }) ?? new[] { new { endpoint = e.Key, details = (object)new { status = e.Value.Status.ToString() } } }
                    ).ToArray(),
                    summary = new
                    {
                        totalChecks = result.Entries.Count,
                        healthy = result.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                        issues = result.Entries.Count(e => e.Value.Status != HealthStatus.Healthy)
                    }
                };

                var statusCode = result.Status == HealthStatus.Healthy ? 200 : 503;
                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API endpoints health check failed");
                return StatusCode(503, new
                {
                    status = "Error",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    error = "API endpoints health check failed"
                });
            }
        }

        /// <summary>
        /// Application components health check - accessible at /health/components
        /// </summary>
        [HttpGet("/health/components")]
        public async Task<IActionResult> ApplicationComponentsHealth()
        {
            try
            {
                var result = await _healthCheckService.CheckHealthAsync(check => 
                    check.Tags.Contains("components"));
                
                var response = new
                {
                    status = result.Status.ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    components = result.Entries.SelectMany(e => 
                        e.Value.Data?.Select(d => new
                        {
                            component = d.Key,
                            details = d.Value
                        }) ?? new[] { new { component = e.Key, details = (object)new { status = e.Value.Status.ToString() } } }
                    ).ToArray(),
                    summary = new
                    {
                        totalComponents = result.Entries.SelectMany(e => e.Value.Data ?? new Dictionary<string, object>()).Count(),
                        available = result.Entries.SelectMany(e => e.Value.Data ?? new Dictionary<string, object>())
                            .Count(d => d.Value.ToString().Contains("Available") || d.Value.ToString().Contains("true")),
                        issues = result.Entries.Count(e => e.Value.Status != HealthStatus.Healthy)
                    }
                };

                var statusCode = result.Status == HealthStatus.Healthy ? 200 : 503;
                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application components health check failed");
                return StatusCode(503, new
                {
                    status = "Error",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    error = "Application components health check failed"
                });
            }
        }

        /// <summary>
        /// Critical systems health check - accessible at /health/critical
        /// </summary>
        [HttpGet("/health/critical")]
        public async Task<IActionResult> CriticalSystemsHealth()
        {
            try
            {
                var result = await _healthCheckService.CheckHealthAsync(check => 
                    check.Tags.Contains("critical"));
                
                var response = new
                {
                    status = result.Status.ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    criticalSystems = result.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        healthy = e.Value.Status == HealthStatus.Healthy,
                        responseTime = $"{e.Value.Duration.TotalMilliseconds:F0}ms",
                        description = e.Value.Description,
                        error = e.Value.Exception?.Message
                    }).ToArray(),
                    summary = new
                    {
                        allCriticalHealthy = result.Status == HealthStatus.Healthy,
                        totalCritical = result.Entries.Count,
                        healthy = result.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                        unhealthy = result.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy)
                    }
                };

                var statusCode = result.Status == HealthStatus.Healthy ? 200 : 503;
                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical systems health check failed");
                return StatusCode(503, new
                {
                    status = "Error",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    error = "Critical systems health check failed"
                });
            }
        }

        private string GetApplicationVersion()
        {
            return Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "1.0.0";
        }

        private string GetUptime()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var uptime = DateTime.UtcNow.Subtract(process.StartTime.ToUniversalTime());
                return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
