using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.BGService
{
    public interface IJob
    {
        Task ExecuteAsync(CancellationToken token);
    }

    public class OverdueRepaymentJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OverdueRepaymentJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task ExecuteAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();

            var processor = scope.ServiceProvider
                .GetRequiredService<RepaymentOverdueProcessor>();

            await processor.ProcessOverdueRepaymentsAsync(token);
        }
    }

    public class JobSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _provider;

        public JobSchedulerService(IServiceProvider provider)
        {
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOverdueJob(stoppingToken);

                // run once every hour (adjust later)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task RunOverdueJob(CancellationToken token)
        {
            using var scope = _provider.CreateScope();

            var job = scope.ServiceProvider.GetRequiredService<OverdueRepaymentJob>();

            await job.ExecuteAsync(token);
        }
    }
}
