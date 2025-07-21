using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Text.Json;

namespace Agrovesto.Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Basic health check endpoint - returns simple OK if healthy
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = result.Status.ToString(),
                timestamp = DateTime.UtcNow
            };

            return result.Status == HealthStatus.Healthy 
                ? Ok(response) 
                : StatusCode((int)HttpStatusCode.ServiceUnavailable, response);
        }

        /// <summary>
        /// Detailed health check endpoint with individual service status
        /// </summary>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed()
        {
            var result = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = result.Status.ToString(),
                timestamp = DateTime.UtcNow,
                totalDuration = result.TotalDuration,
                checks = result.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration,
                    description = entry.Value.Description,
                    exception = entry.Value.Exception?.Message,
                    data = entry.Value.Data
                })
            };

            return result.Status == HealthStatus.Healthy 
                ? Ok(response) 
                : StatusCode((int)HttpStatusCode.ServiceUnavailable, response);
        }

        /// <summary>
        /// Ready endpoint - checks if application is ready to serve traffic
        /// </summary>
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            var result = await _healthCheckService.CheckHealthAsync("ready");

            var response = new
            {
                status = result.Status.ToString(),
                timestamp = DateTime.UtcNow
            };

            return result.Status == HealthStatus.Healthy 
                ? Ok(response) 
                : StatusCode((int)HttpStatusCode.ServiceUnavailable, response);
        }

        /// <summary>
        /// Live endpoint - checks if application is alive (basic liveness probe)
        /// </summary>
        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                message = "Application is alive"
            });
        }
    }
}
