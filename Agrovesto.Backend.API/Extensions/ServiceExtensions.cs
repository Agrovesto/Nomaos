using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.LoggingService;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;

namespace Agrovesto.Backend.API.Extensions
{
    public static class ServicesExtensions
    {
        public static void AddAppServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddDataProtection();


            services.AddLogging(config => config.AddConsole());

            services.AddHttpContextAccessor();

            services.AddScoped<IHttpService, HttpService>();
            services.AddSingleton(configuration.GetSection("AppLogs").Get<SerilogServiceOptions>());
            services.AddSingleton<ILogService, SerilogService>();
           
            services.AddScoped<IBusinessRepository, BusinessRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
            services.AddScoped<IWalletProviderRepository, WalletProviderRepository>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IFarmerRepository, FarmerRepository>();
            services.AddScoped<IAgentRepository, AgentRepository>();
            services.AddScoped<ICooperativeRepository, CooperativeRepository>();
           

            services.AddScoped<IBusinessManager, BusinessManager>();
            services.AddScoped<IRoleManager, RoleManager>();
            services.AddScoped<IWalletProviderManager, WalletProviderManager>();
            services.AddScoped<IWalletManager, WalletManager>();


            services.AddScoped<ICafeOneWalletService, CafeOneWalletService>();

        }
    }
}
