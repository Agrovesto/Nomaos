using System;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;


namespace Agrovesto.Backend.Infrastructure.ExternalServices.BGService
{
    
    public class FarmerOrganizationSyncService : IHostedService
    {
        private readonly ILogger<FarmerOrganizationSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public FarmerOrganizationSyncService(IServiceProvider serviceProvider, ILogger<FarmerOrganizationSyncService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FarmerOrganizationSyncService started.");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                // Get all farmers
                var farmers = await dbContext.Farmers
                    .Select(f => new { f.Id, f.UserId })
                    .ToListAsync(cancellationToken);

                // Get all UserOrganizations
                var userOrgs = await dbContext.UserOrganizations
                    .Select(uo => new { uo.UserId, uo.OrganizationCode })
                    .ToListAsync(cancellationToken);

                // Get existing FarmerOrganizations
                var existingFarmerOrgs = await dbContext.FarmerOrganizations
                    .Select(fo => new { fo.FarmerId, fo.OrganizationCode })
                    .ToListAsync(cancellationToken);

                var newEntries = new List<FarmerOrganization>();

                foreach (var farmer in farmers)
                {
                    var orgCodes = userOrgs
                        .Where(uo => uo.UserId == farmer.UserId)
                        .Select(uo => uo.OrganizationCode);

                    foreach (var orgCode in orgCodes)
                    {
                        // Skip if already exists
                        bool exists = existingFarmerOrgs
                            .Any(fo => fo.FarmerId == farmer.Id && fo.OrganizationCode == orgCode);

                        if (!exists)
                        {
                            newEntries.Add(new FarmerOrganization
                            {
                                FarmerId = farmer.Id,
                                OrganizationCode = orgCode,
                                OnBoardedOn = DateTimeOffset.UtcNow
                            });
                        }
                    }
                }

                if (newEntries.Any())
                {
                    await dbContext.FarmerOrganizations.AddRangeAsync(newEntries, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation($"{newEntries.Count} FarmerOrganizations entries created for existing farmers.");
                }
                else
                {
                    _logger.LogInformation("All existing farmers already have FarmerOrganizations entries.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing FarmerOrganizations.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FarmerOrganizationSyncService stopped.");
            return Task.CompletedTask;
        }
    }

}
