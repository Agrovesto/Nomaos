using Agrovesto.Backend.API.Extensions;
using Agrovesto.Backend.API.HealthChecks;
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
using AWS.Logger;
using AWS.Logger.AspNetCore;
using AWS.Logger.SeriLog;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
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
using VaultSharp;
using VaultSharp.Extensions.Configuration;
using VaultSharp.V1.AuthMethods.AppRole;


var builder = WebApplication.CreateBuilder(args);


var logConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console();
var logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH");
if (!string.IsNullOrEmpty(logFilePath))
{
    logConfig.WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day);
}
Log.Logger = logConfig
    .WriteTo.AWSSeriLog(new AWSLoggerConfig
    {
        LogGroup = "/nomaos/prod/api",
        Region = "eu-west-1",
    })
    .CreateLogger();

builder.Host.UseSerilog();



//var vaultUrl = "https://dev-vault.agrovesto.com"; //local

//var roleId = "69d8526c-0bf6-56c8-40cd-8e8b37c213b4"; //local
//var secretId = "444fa57a-84a9-5665-857d-815d8744e1a5"; //local


if (!builder.Environment.IsDevelopment())
{
    var vaultUrl = Environment.GetEnvironmentVariable("VAULT_ADDR");
    var roleId = Environment.GetEnvironmentVariable("VAULT_ROLE_ID");
    var secretId = Environment.GetEnvironmentVariable("VAULT_SECRET_ID");

    if (!string.IsNullOrEmpty(roleId) &&
        !string.IsNullOrEmpty(secretId) &&
        !string.IsNullOrEmpty(vaultUrl))
    {
        ((IConfigurationBuilder)builder.Configuration).AddVaultConfiguration(
            () => new VaultOptions(
                vaultUrl,
                new AppRoleAuthMethodInfo(roleId, secretId),
                reloadOnChange: true,
                reloadCheckIntervalSeconds: 60
            ),
            "nomaos/prod/appsettings",
            "kv"
        );
    }
}
// Load AWS Secrets (if configured) into the application's configuration early so services can consume them during registration
Console.WriteLine($"Loading AWS Secrets... config {builder.Configuration.GetConnectionString("DefaultConnection")}");

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
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

builder.Services.Configure<IdentitypassOptions>(
    builder.Configuration.GetSection("IdentityPass"));

builder.Services.Configure<AesOptions>(
    builder.Configuration.GetSection("AesOptions"));

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<BulkUploadBgService>();
builder.Services.AddHostedService<FarmerOrganizationSyncService>();
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
    .AddDbContextCheck<DataContext>("Database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "database", "critical" },
        timeout: TimeSpan.FromSeconds(5))
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "SQL Server Database",
        healthQuery: "SELECT 1",
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "database", "critical" })

    .AddCheck<IdentityPassHealthCheck>("IdentityPass API",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external" },
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<TermiiSmsHealthCheck>("Termii SMS API",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external" },
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<CountryStateCityApiHealthCheck>("Country State City API",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external" },
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<SendGridHealthCheck>("SendGrid API",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external" },
        timeout: TimeSpan.FromSeconds(5))
    .AddS3(options =>
    {
        options.BucketName = builder.Configuration["AWS:BucketName"];
        options.AccessKey = builder.Configuration["AWS:AccessKey"];
        options.SecretKey = builder.Configuration["AWS:SecretKey"];
    }, name: "AWS S3",
       failureStatus: HealthStatus.Degraded,
       tags: new[] { "external", "storage" },
       timeout: TimeSpan.FromSeconds(5))

    .AddCheck<ApiEndpointHealthCheck>("API Endpoints",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "api", "endpoints" },
        timeout: TimeSpan.FromSeconds(10))
    .AddCheck<ApplicationComponentHealthCheck>("Application Components",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "components", "critical" });

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
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

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

var customHealthResponseWriter = async (HttpContext context, HealthReport report) =>
{
    context.Response.ContentType = "application/json";
    var result = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration.TotalMilliseconds,
            data = e.Value.Data
        })
    };

    await context.Response.WriteAsJsonAsync(result);
};

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    // Match the documented "graceful degradation" design: a single degraded external
    // dependency must not flip the whole endpoint to 503. Only critical system
    // failures (e.g. Application Components) return Unhealthy -> 503.
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
    ResponseWriter = async (ctx, report) => await customHealthResponseWriter(ctx, report)
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("critical"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
    ResponseWriter = async (ctx, report) => await customHealthResponseWriter(ctx, report)
});
app.UseHttpsRedirection();

// Enable static files for health dashboard
app.UseStaticFiles();
app.UseHangfireDashboard("/hangfire");
app.UseAuthentication();
//app.UseMiddleware<LicenseValidationMiddleware>();// remove before pushing
//app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
