using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.BGService
{
   
    public class BulkUploadBgService : BackgroundService
    {
        private readonly ILogger<BulkUploadBgService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBackgroundTaskQueue _taskQueue;
        public BulkUploadBgService(ILogger<BulkUploadBgService> logger, IServiceScopeFactory scopeFactory, IBackgroundTaskQueue taskQueue)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _taskQueue = taskQueue;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Service Started");
            // On startup, re-enqueue any unfinished jobs persisted in the database
            try
            {
                using var startupScope = _scopeFactory.CreateScope();
                var db = startupScope.ServiceProvider.GetRequiredService<DataContext>();

                var unfinished = await db.BulkUploadJobs
                    .Where(j => j.Status == "Queued" || j.Status == "Processing")
                    .ToListAsync(stoppingToken);

                foreach (var job in unfinished)
                {
                    _logger.LogInformation("Re-enqueueing unfinished job {job}", job.Id);
                    _taskQueue.QueueTask(async token =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var processor = scope.ServiceProvider.GetRequiredService<BulkOnboardingProcessor>();
                        await processor.ProcessJobAsync(job.Id, token);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-enqueue unfinished bulk upload jobs on startup");
            }
            // Implement your background service logic here
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Background task running at: {time}", DateTime.UtcNow);
                // Perform background work
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                await workItem(stoppingToken);
            }
            _logger.LogInformation("Background Service Stopped");
        }
    }
}
