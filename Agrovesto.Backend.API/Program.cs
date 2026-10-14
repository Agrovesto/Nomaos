using Agrovesto.Backend.API.Extensions;
using Agrovesto.Backend.API.Health;
using Agrovesto.Backend.API.HealthChecks;
using Agrovesto.Backend.API.Observability;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon;
using Amazon.Rekognition;
using Amazon.S3;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using Serilog;
using SixLabors.ImageSharp;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using VaultSharp;
using VaultSharp.Extensions.Configuration;
using VaultSharp.V1.AuthMethods.AppRole;


var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// Logging — structured JSON on stdout.
//
// The container's stdout is the log transport (Docker json-file -> Grafana
// Alloy -> Loki), so every line is a single JSON object and every line carries
// trace_id/span_id (ActivityTraceEnricher) for the Loki -> Tempo jump.
//
// writeToProviders: true is what lets the OpenTelemetry logging provider see
// the same events, so OTLP log records and stdout lines never disagree.
// See NomaosSerilog for the LOG_LEVEL / LOG_FILE_PATH knobs.
// ─────────────────────────────────────────────────────────────────────────────
Log.Logger = NomaosSerilog.CreateBootstrapLogger();

builder.Host.UseSerilog(
    (context, _, loggerConfiguration) => NomaosSerilog.Configure(
        loggerConfiguration,
        Environment.GetEnvironmentVariable,
        context.HostingEnvironment.EnvironmentName),
    preserveStaticLogger: false,
    writeToProviders: true);




/**
if (!builder.Environment.IsDevelopment())
{
var vaultUrl = Environment.GetEnvironmentVariable("VAULT_ADDR");
    var roleId = Environment.GetEnvironmentVariable("VAULT_ROLE_ID");
    var secretId = Environment.GetEnvironmentVariable("VAULT_SECRET_ID");
    var vaultMount = Environment.GetEnvironmentVariable("VAULT_KV_MOUNT") ?? "kv";
    var vaultSecretPath = Environment.GetEnvironmentVariable("VAULT_SECRET_PATH");


    if (string.IsNullOrWhiteSpace(vaultUrl))
        throw new InvalidOperationException("VAULT_ADDR is not configured.");
    if (string.IsNullOrWhiteSpace(roleId))
        throw new InvalidOperationException("VAULT_ROLE_ID is not configured.");
    if (string.IsNullOrWhiteSpace(secretId))
        throw new InvalidOperationException("VAULT_SECRET_ID is not configured.");
    if (string.IsNullOrWhiteSpace(vaultSecretPath))
        throw new InvalidOperationException("VAULT_SECRET_PATH is not configured.");
    {

        ((IConfigurationBuilder)builder.Configuration).AddVaultConfiguration(
            () => new VaultOptions(
                vaultUrl,
                new AppRoleAuthMethodInfo(roleId, secretId),
                reloadOnChange: true,
                reloadCheckIntervalSeconds: 60
            ),
            vaultSecretPath,
            vaultMount
        );
    }
}**/



// ─────────────────────────────────────────────────────────────────────────────
// OpenTelemetry — traces + metrics to Alloy (Tempo / Prometheus), driven purely
// by the OTEL_* environment block. Registered after the Vault provider so
// OTEL_* values could also come from Vault if an operator puts them there.
// ─────────────────────────────────────────────────────────────────────────────
var observabilityOptions = ObservabilityOptions.FromConfiguration(
    builder.Configuration,
    builder.Environment.EnvironmentName,
    typeof(Program).Assembly);

builder.Services.AddNomaosObservability(observabilityOptions);
// Configuration provenance, without the payload: the previous version printed
// the full DefaultConnection string (server, user id AND password) to stdout,
// which now means shipping database credentials straight into Loki/CloudWatch.
Log.Information(
    "Configuration loaded. DefaultConnection configured: {HasConnectionString}. Vault configured: {HasVault}",
    !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")),
    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VAULT_ADDR")));

// Whether this process has a database to talk to at all. In every deployed
// environment the connection string arrives from Vault, so this is true; it is
// false only for the CI container smoke test (and a bare `docker run`), where
// the image must still boot far enough to answer /health/live. Without this
// flag the Hangfire server, the sync hosted services and the SQL health check
// all fail during startup and the process never starts listening — which would
// make the smoke test assert on a container that died for the wrong reason.
var databaseConfigured = !string.IsNullOrWhiteSpace(
    builder.Configuration.GetConnectionString("DefaultConnection"));
Console.WriteLine($"Database configured: {builder.Configuration.GetConnectionString("DefaultConnection")}");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});


builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("Agrovesto.Backend.API");
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(300);
            sqlOptions.UseNetTopologySuite(); 
        }
    )
);
// Hangfire keeps its state in SQL Server and its storage provider connects
// (and creates its schema) during startup, so it is only wired when there is a
// database configured.
var hangfireEnabled = databaseConfigured
    && !string.Equals(builder.Configuration["HANGFIRE_ENABLED"], "false", StringComparison.OrdinalIgnoreCase);

if (hangfireEnabled)
{
    builder.Services.AddHangfire(config =>
        config.UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHangfireServer();
}
else
{
    Log.Warning("Hangfire is disabled: no DefaultConnection configured (or HANGFIRE_ENABLED=false).");
}




builder.Services.Configure<IdentitypassOptions>(
    builder.Configuration.GetSection("IdentityPass"));

builder.Services.Configure<AesOptions>(
    builder.Configuration.GetSection("AesOptions"));

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

// Both of these query the database inside StartAsync, which runs before Kestrel
// starts listening. With EnableRetryOnFailure that is a multi-minute stall when
// the database is unreachable, so they are only registered when one is
// configured.
if (databaseConfigured)
{
    builder.Services.AddHostedService<BulkUploadBgService>();
    builder.Services.AddHostedService<FarmerOrganizationSyncService>();
}
builder.Services.AddScoped<BulkOnboardingProcessor>();
builder.Services.Configure<ReconciliationEmailSettings>(
    builder.Configuration.GetSection("ReconciliationEmailSettings"));

builder.Services.AddIdentity<User, Role>(opt =>
{
    opt.SignIn.RequireConfirmedAccount = true;
    // opt.User.RequireUniqueEmail = true;
    opt.Password.RequireDigit = false;
    opt.Password.RequiredLength = 4;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase = false;
    opt.Password.RequireLowercase = false;
})

// .AddRoles<Role>()
// .AddRoleManager<RoleManager<Role>>()
//.AddSignInManager<SignInManager<User>>()
//.AddUserManager<UserManager<User>>()
.AddEntityFrameworkStores<DataContext>()
.AddDefaultTokenProviders();

// The admin merge makes AspNetUserRoles business-scoped; Identity's stock EF store
// does not populate the required BusinessId or understand the three-column key.
builder.Services.AddScoped<IUserStore<User>, BusinessScopedUserStore>();

builder.Services.AddOptions<AdminPanelOptions>()
    .Bind(builder.Configuration.GetSection("AdminPanel"))
    .Validate(o => o.BusinessId > 0 && !string.IsNullOrWhiteSpace(o.PortalBaseUrl),
        "AdminPanel:BusinessId and AdminPanel:PortalBaseUrl must be configured.");

builder.Services.AddAppServices(builder.Configuration);

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonRekognition>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
        ),
        RoleClaimType = "Roles"
    }; 
    opt.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

            if (string.IsNullOrEmpty(jti))
            {
                context.Fail("Token missing jti claim.");
                return;
            }

            var revocationService = context.HttpContext.RequestServices
                .GetRequiredService<IOnboardingRepository>();

            if (await revocationService.IsRevokedAsync(jti))
            {
                context.Fail("Token has been revoked.");
            }
        }
    };
});


builder.Services.Configure<FormOptions>(o =>
{
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartBodyLengthLimit = int.MaxValue;
    o.MemoryBufferThreshold = int.MaxValue;
});
QuestPDF.Settings.License = LicenseType.Community;


builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("admin-login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.Converters.Add(new EmptyStringToNullDateTimeOffsetConverter());
    opts.JsonSerializerOptions.Converters.Add(new EmptyStringToNullStringConverter());

});

// Dedicated HttpClient for external health checks with a sensible timeout so a slow upstream
// can never stall the /health endpoint. The default HttpClient timeout is 100s, which is
// way too long for a health probe.
builder.Services.AddHttpClient("health", c =>
{
    c.Timeout = TimeSpan.FromSeconds(5);
    c.DefaultRequestHeaders.Add("User-Agent", "Agrovesto-HealthCheck/1.0");
});

// External health checks use failureStatus: Degraded so a single flaky third-party service
// cannot take down the whole /health endpoint (matches the documented "graceful degradation"
// design in HealthCheck-README.md). All non-DB checks are also bounded by a 5s per-check
// timeout so a hung probe cannot stall the health response. Database and application
// component checks remain strict (Unhealthy on failure) because they are tagged
// "critical" and the load balancer should pull the instance out of rotation if
// those are down.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DataContext>(
        "Database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "database", "critical" })
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "SQL Server Database",
        healthQuery: "SELECT 1",
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "database", "critical" })
    .AddCheck<IdentityPassHealthCheck>(
        "IdentityPass API",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", HealthTags.Status },
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<TermiiSmsHealthCheck>(
        "Termii SMS API",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", HealthTags.Status },
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<CountryStateCityApiHealthCheck>(
        "Country State City API",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", HealthTags.Status },
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<SendGridHealthCheck>(
        "SendGrid API",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", HealthTags.Status },
        timeout: TimeSpan.FromSeconds(5))
    .AddS3(
        options =>
        {
            options.BucketName = builder.Configuration["AWS:BucketName"];
            options.AccessKey = builder.Configuration["AWS:AccessKey"];
            options.SecretKey = builder.Configuration["AWS:SecretKey"];
        },
        name: "AWS S3",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "storage" },
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<ApiEndpointHealthCheck>(
        "API Endpoints",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "api", "endpoints", HealthTags.Status },
        timeout: TimeSpan.FromSeconds(10))
    .AddCheck<ApplicationComponentHealthCheck>(
        "Application Components",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "components", "critical", HealthTags.Status });

builder.Services.AddScoped<IdentityPassHealthCheck>();
builder.Services.AddScoped<TermiiSmsHealthCheck>();
builder.Services.AddScoped<CountryStateCityApiHealthCheck>();
builder.Services.AddScoped<SendGridHealthCheck>();
builder.Services.AddScoped<ApiEndpointHealthCheck>();
builder.Services.AddScoped<ApplicationComponentHealthCheck>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo
    { 
        Title = "NOMAOS Platform API",
        Version = "v1" 
    });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
  
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
      
    });

});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
// Serilog owns stdout (JSON lines). Clearing the providers removes the default
// plain-text console provider that would otherwise print every event a second
// time in a non-parseable format; the OpenTelemetry provider is then the only
// MEL provider, receiving events via Serilog's writeToProviders bridge.
builder.Logging.ClearProviders();
builder.Logging.AddNomaosOpenTelemetryLogging(observabilityOptions);

var corsOrigins = builder.Configuration
    .GetValue<string>("CorsOrigins")?
    .Split(",") ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder
                   .WithOrigins(corsOrigins)
                   .AllowAnyMethod()
                   .AllowCredentials()
                   .AllowAnyHeader()
                   .SetIsOriginAllowedToAllowWildcardSubdomains()
                   .Build();
        });
});


var app = builder.Build();

if (databaseConfigured)
{
    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        await Agrovesto.Backend.Infrastructure.Logic.Managers.PermissionSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        // Missing/unapplied admin-panel migrations should not take down the core API.
        Log.Error(ex, "Admin permission seeding failed; verify the database migrations and role data.");
    }
}

AppSettingsHelper.AppSettingConfigure(app.Services.GetRequiredService<IConfiguration>());
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

  
   
    
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NomaOS ");
        c.DocumentTitle = "NOMAOS Platform API";
    

    });
}



app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors();
app.UseRouting();
app.UseRateLimiter();

// ─────────────────────────────────────────────────────────────────────────────
// Probe surface: /health/live (self only), /health/ready (SQL + Redis + Vault),
// /health (legacy summary) and /status (ops detail, always 200).
// See Agrovesto.Backend.API/Health/HealthCheckExtensions.cs for the contract.
// ─────────────────────────────────────────────────────────────────────────────
app.MapNomaosHealthEndpoints();

// Manual span + custom counter/histogram demo (traces -> Tempo, metrics -> Prometheus).
app.MapObservabilityDemoEndpoint();

app.UseHttpsRedirection();

// Enable static files for health dashboard
app.UseStaticFiles();
if (hangfireEnabled)
{
    app.UseHangfireDashboard("/hangfire");
}
app.UseAuthentication();
//app.UseMiddleware<LicenseValidationMiddleware>();// remove before pushing
//app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
