using Agrovesto.Backend.Core;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.ExternalServices.ImageRecognition;
using Agrovesto.Backend.Infrastructure.ExternalServices.LoggingService;
using Agrovesto.Backend.Infrastructure.ExternalServices.MarketDataAnalyticsService;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.ExternalServices.PaymentService;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Agrovesto.Backend.Infrastructure.Logic;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.MandEService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Logic.Services.MandE;
using Agrovesto.Backend.Infrastructure.Logic.Services.RolePermissionService;
using Agrovesto.Backend.Infrastructure.Utilities;
using Agrovesto.Backend.Infrastructure.V2.Internal;

namespace Agrovesto.Backend.API.Extensions
{
    public static class ServicesExtensions
    {
        public static void AddAppServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddDataProtection();


            // Logging providers are configured once in Program.cs: Serilog owns
            // stdout (JSON lines) and OpenTelemetry owns OTLP export. Re-adding
            // the plain-text console provider here would duplicate every line in
            // a format Loki cannot parse.
            services.AddLogging();

            services.AddHttpContextAccessor();

            services.AddScoped<IHttpService, HttpService>();
            services.Configure<SerilogServiceOptions>(
    configuration.GetSection("AppLogs"));
            //services.AddSingleton(configuration.GetSection("AppLogs").Get<SerilogServiceOptions>());

            services.AddSingleton<ILogService, SerilogService>();
           

            services.AddScoped<IBulkFarmerUploadService, BulkFarmerUploadService>();
            services.AddScoped<IBulkMembershipValidator, BulkMembershipValidator>();
            services.AddScoped<IBulkMembershipExcelParser, BulkMembershipExcelParser>();
            services.AddScoped<IBulkMembershipUploadService, BulkMembershipUploadService>();
            services.AddScoped<IBusinessRepository, BusinessRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
            services.AddScoped<IWalletProviderRepository, WalletProviderRepository>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IFarmerRepository, FarmerRepository>();
            services.AddScoped<IAgentRepository, AgentRepository>();
            services.AddScoped<IMembershipRepository, MembershipRepository>();
            services.AddScoped<IFarmPlotDetailRepository, FarmPlotDetailRepository>();
            services.AddScoped<IWarehouseManagementRepository, WarehouseManagementRepository>();
            services.AddScoped<IInputFinancingManagementRepository, InputFinancingManagementRepository>();
           services.AddScoped<IFarmerTransactionRepository, FarmerTransactionRepository>();
            services.AddScoped<ITraderRepository, TraderRepository>();
            services.AddScoped<IPaymentProviderRepository, PaymentProviderRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IFarmerCreditScoreRepository, FarmerCreditScoreRepository>();
            services.AddScoped<IOrganizationConfigurationRepository, OrganizationConfigurationRepository>();
            services.AddScoped<IInputFinancingSchemeRepository, InputFinancingSchemeRepository>();
            services.AddScoped<IFarmRepository, FarmRepository>();

            services.AddScoped<IBusinessManager, BusinessManager>();
            services.AddScoped<IRoleManager, RoleManager>();
            services.AddScoped<IWalletProviderManager, WalletProviderManager>();
            services.AddScoped<IWalletManager, WalletManager>();
            services.AddScoped<IFarmPlotDetailsManager, FarmPlotDetailsManager>();
            services.AddScoped<IWarehouseManagementManager, WarehouseManagementManager>();
            services.AddScoped<IInputFinancingManagement, InputFinancingManagement>();
            services.AddScoped<IAWSRekognitionManager, AWSRekgnitionManager>();
            services.AddScoped<IFarmerTransactionManager, FarmerTransactionManager>();
            services.AddScoped<IReportManager, ReportManager>();
            services.AddScoped<IPaymentProviderManager, PaymentProviderManager>();
            services.AddScoped<IDashboardManager, DashboardManager>();
            services.AddScoped<IPayoutManager, PayoutManager>();
            services.AddScoped<IFarmerCreditScoremanager, FarmerCreditScoremanager>();
            services.AddScoped<IWalletSetUpManager, WalletSetUpManager>();
            services.AddScoped<ISettingsManager, SettingsManager>();
            services.AddScoped<IInputFinancingSchemeManager, InputFinancingSchemeManager>();
            services.AddScoped<IFarmManager, FarmManager>();
            services.AddScoped<IAgentManager, AgentManager>();
            services.AddScoped<IMembershipManager, MembershipManager>();
            services.AddScoped<IFarmMappingManager, FarmMappingManager>();
            services.AddScoped<IOrganizationManager, OrganizationManager>();



            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IIdentityVerificationService, IdentityVerificationService>();
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.AddScoped<IExcelConverterService, ExcelConverterService>();
            services.AddScoped<IFlutterwaveService, FlutterwavePaymentService>();
            services.AddScoped<IGoogleDriveService, GoogleDriveService>();
            services.AddScoped<INigeriaStateImporterService, NigeriaStateImporterService>();
            services.AddScoped<IRepaymentEngine, RepaymentEngine>();
         
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IOnBoardingManager, OnBoardingManager>();
            services.AddScoped<IOnboardingRepository, OnboardingRepository>();
            services.AddScoped<IFarmMappingRepository, FarmMappingRepository>();
            //services.AddScoped<IFarmerCategoryService, FarmerCategoryService>();
           




            services.AddScoped<IMandERepositories, MandERepositories>();
            services.AddScoped<IActivityEntryRepository, ActivityEntryRepository>();
            services.AddScoped<IMandEActivityManager, MandEActivityManager>();
            services.AddScoped<IMandEServiceRepository, MandEServiceRepositories>();
            services.AddScoped<IMandEFormulaEngine, MandEFormulaEngine>();
            services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();



            //services.AddHttpClient<SterlingWalletService>();
            //services.AddHttpClient<FCMBWalletService>();
            //services.AddHttpClient<CafeOneWalletService>();
            //  services.AddHttpClient<CredoService>();
            services.AddScoped<IFarmerQueryService, FarmerQueryService>();
            services.AddScoped<IUserWalletRepository, UserWalletRepository>();
            services.AddScoped<INomaOSWalletService, NomaOSWalletService>();
            // Concrete registrations - required for WalletServiceFactory to resolve them
           
           
            services.AddScoped<IGenericWalletService, MTNMomo>(); 
            services.AddScoped<IGenericWalletService, EtransactWallet>();

            // Interface registrations - only needed if something else in your code
            // depends on IMTNMomo / IEtransactWallet directly
            services.AddScoped<IMTNMomo, MTNMomo>();
            services.AddScoped<IEtransactWallet, EtransactWallet>();


            // The factory itself
            services.AddScoped<IWalletServiceFactory, WalletServiceFactory>();
            // services.AddScoped<IWalletServiceFactory, WalletServiceFactory>();



            services.AddScoped<ICafeOneWalletService, CafeOneWalletService>();
            services.AddScoped<IWalletService, FCMBWalletService>();
            services.AddScoped<IWalletService, SterlingWalletService>();
          //  services.AddScoped<MTNMomo>();
           // services.AddScoped<IMTNMomo, MTNMomo>();

          //  services.AddScoped<IGenericWalletService, MTNMomo>();
            services.AddScoped<IAWSService, AWSService>();
            services.AddScoped<ISalesRepository, SalesRepository>();

            services.AddScoped<IWebhookRouter, WebhookRouter>();

            services.AddScoped<IProviderWebhookHandler, PaystackWebhookHandler>();
            services.AddScoped<IProviderWebhookHandler, FlutterwaveWebhookHandler>();

            services.AddScoped<IProviderWebhookHandler, MtnMomoWebhookHandler>();
            services.AddScoped<IProviderWebhookHandler, SterlingWebhookHandler>();
            services.AddScoped<IProviderWebhookHandler, FcmbWebhookHandler>();

            services.AddScoped<IStorageCategoryRepository, StorageCategoryRepository>();
            services.AddScoped<ITemperatureControlType, TemperatureControlTypeRepository>();
            services.AddScoped<IModuleRepository, ModuleRepository>();
            services.AddScoped<IInputFinancingManager, InputFinancingManager>();
            services.AddScoped<IInputFinancingRepository, InputFinancingRepository>();

            services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
            services.AddScoped<ILicenseService, LicenseService>();
            services.AddScoped<IModuleAccessService, SystemModuleAccessService>();
            services.AddScoped<IMigrationService, MigrationService>();
            services.AddScoped<IProfileAccessPolicy, DefaultProfileAccessPolicy>();

            services.AddScoped<RepaymentOverdueProcessor>();
            services.AddHostedService<JobSchedulerService>();

            services.AddTransient<OverdueRepaymentJob>();


            services.Configure<RateLimitSettingsUtil>(configuration.GetSection("RateLimiting"));
            services.AddSingleton<IRateLimitService, RateLimitServiceUtil>();
            services.AddScoped<ISMSProviderRepository, SMSProviderRepository>();
            services.AddScoped<IUserRoleReconciliationService, UserRoleReconciliationService>();


            services.AddScoped<IResetRateLimiter, ResetRateLimiter>();
            services.AddScoped<ICredentialChangeHandler, PasswordChangeHandler>();
            services.AddScoped<ICredentialChangeHandler, PinChangeHandler>();
            services.AddScoped<CredentialChangeService>();

            // Admin portal services, permission checks, audit trail, and export support.
            services.AddMemoryCache();
            services.AddScoped<IAdminDashboardManager, AdminDashboardManager>();
            services.AddScoped<IAdminUserManagementManager, AdminUserManagementManager>();
            services.AddScoped<IAdminFarmerManager, AdminFarmerManager>();

            services.AddScoped<IAdminOrganizationManager, AdminOrganizationManager>();
            services.AddScoped<IAdminAgentManager, AdminAgentManager>();
            services.AddScoped<IAdminMembershipManager, AdminMembershipManager>();

            services.AddScoped<IRoleAccessManager, RoleAccessManager>();
            services.AddScoped<IAdminAuditLogger, AdminAuditLogger>();
            services.AddScoped<IInputFinanceReader, EfInputFinanceReader>();
            services.AddScoped<IClientInfoProvider, ClientInfoProvider>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddSingleton<IPermissionCacheInvalidator, PermissionCacheInvalidator>();
            services.AddScoped<IAdminSettingsManager, AdminSettingsManager>();

            services.Configure<ProfileImageOptions>(configuration.GetSection("ProfileImages"));
            services.AddSingleton<IProfileImageStore, S3ProfileImageStore>();
        }
    }
}
