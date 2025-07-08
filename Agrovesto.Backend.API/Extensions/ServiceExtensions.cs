using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.ImageRecognition;
using Agrovesto.Backend.Infrastructure.ExternalServices.LoggingService;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.ExternalServices.PaymentService;
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
            services.AddScoped<IFarmPlotDetailRepository, FarmPlotDetailRepository>();
            services.AddScoped<IWarehouseManagementRepository, WarehouseManagementRepository>();
            services.AddScoped<IInputFinancingManagementRepository, InputFinancingManagementRepository>();
           services.AddScoped<IFarmerTransactionRepository, FarmerTransactionRepository>();
            services.AddScoped<ITraderRepository, TraderRepository>();
            services.AddScoped<IPaymentProviderRepository, PaymentProviderRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IFarmerCreditScoreRepository, FarmerCreditScoreRepository>();

            services.AddScoped<IBusinessManager, BusinessManager>();
            services.AddScoped<IRoleManager, RoleManager>();
            services.AddScoped<IWalletProviderManager, WalletProviderManager>();
            services.AddScoped<IWalletManager, WalletManager>();
            services.AddScoped<IFarmPlotDetailsManager, FarmPlotDetailsManager>();
            services.AddScoped<IWarehouseManagementManager, WarehouseManagementManager>();
            services.AddScoped<IInputFinancingManagement, InputFinancingManagement>();
            services.AddScoped<IFaceManager, FaceManager>();
            services.AddScoped<IFarmerTransactionManager, FarmerTransactionManager>();
            services.AddScoped<IReportManager, ReportManager>();
            services.AddScoped<IPaymentProviderManager, PaymentProviderManager>();
            services.AddScoped<IDashboardManager, DashboardManager>();
            services.AddScoped<IPayoutManager, PayoutManager>();
            services.AddScoped<IFarmerCreditScoremanager, FarmerCreditScoremanager>();


            services.AddScoped<ICafeOneWalletService, CafeOneWalletService>();
            services.AddScoped<ISMSService, SMSService>();
            services.AddScoped<IIdentityVerificationManager, IdentityVerificationManager>();
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.AddScoped<IExcelConverterService, ExcelConverterService>();
            services.AddScoped<IFlutterwaveService, FlutterwavePaymentService>();
            services.AddScoped<IGoogleDriveService, GoogleDriveService>();
        }
    }
}
