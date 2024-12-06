using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Infrastructure.ExternalServices;

namespace Agrovesto.Backend.API.Extensions
{
    public static class ServicesExtensions
    {
        public static void AddAppServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<IHttpService, HttpService>();
            services.AddSingleton<ILogService, SerilogService>();

        }
    }
}
