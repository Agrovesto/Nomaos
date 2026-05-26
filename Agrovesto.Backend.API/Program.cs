using Agrovesto.Backend.API.Extensions;
using Agrovesto.Backend.API.HealthChecks;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon;
using AWS.Logger.SeriLog;
using AWS.Logger;
using AWS.Logger.AspNetCore;
using Amazon.Rekognition;
using Amazon.S3;
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
using Agrovesto.Backend.Infrastructure.DB;
using Serilog;
using System;
using System.Reflection;
using System.Text;
using System.Text.Json;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.Extensions.Configuration;


var builder = WebApplication.CreateBuilder(args);



// =====================
// SERILOG CONFIG
// =====================
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

// =====================
// VAULT CONFIGURATION
// =====================
var vaultUrl = Environment.GetEnvironmentVariable("VAULT_ADDR");

//var vaultUrl = "https://dev-vault.agrovesto.com"; //local

var roleId = Environment.GetEnvironmentVariable("VAULT_ROLE_ID");
//var roleId = "69d8526c-0bf6-56c8-40cd-8e8b37c213b4"; //local
var secretId = Environment.GetEnvironmentVariable("VAULT_SECRET_ID");
//var secretId = "444fa57a-84a9-5665-857d-815d8744e1a5"; //local

// Fail fast if Vault is required in this environment
if (string.IsNullOrWhiteSpace(vaultUrl) ||
    string.IsNullOrWhiteSpace(roleId) ||
    string.IsNullOrWhiteSpace(secretId))
{
    throw new Exception("Vault environment variables are missing. Startup aborted.");
}
try
{
    // =====================
    // Build Vault client
    // =====================
    var authMethod = new AppRoleAuthMethodInfo(roleId, secretId);

    var vaultClientSettings = new VaultClientSettings(vaultUrl, authMethod);

    var vaultClient = new VaultClient(vaultClientSettings);

    // =====================
    // Read secrets from KV
    // =====================
    var secretPath = "nomaos/prod/appsettings";
    var mountPoint = "kv";

    var secretResponse = await vaultClient.V1.Secrets.KeyValue.V2
        .ReadSecretAsync(path: secretPath, mountPoint: mountPoint);

    var data = secretResponse.Data.Data;

    // =====================
    // Inject into Configuration
    // =====================
    var configBuilder = (IConfigurationBuilder)builder.Configuration;

    foreach (var kv in data)
    {
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{kv.Key}"] = kv.Value?.ToString()
        });
    }

    Log.Information("Vault configuration successfully loaded and injected.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to load Vault configuration");
    throw;
}   


// Load AWS Secrets (if configured) into the application's configuration early so services can consume them during registration
// Prefer awaiting the async implementation to avoid blocking threads
//await Agrovesto.Backend.Infrastructure.ExternalServices.AwsSecretsConfigurator.AddSecretsToConfigurationAsync(builder.Configuration);

// Add services to the container.
//builder.Services.AddDbContext<DataContext>(options =>

//              options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
//                  b => b.MigrationsAssembly("Agrovesto.Backend.API"))

//              );

//builder.Services.AddDbContext<DataContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection"),
//        sqlOptions =>
//        {
//            sqlOptions.MigrationsAssembly("Agrovesto.Backend.API");
//            sqlOptions.EnableRetryOnFailure(
//                maxRetryCount: 5,
//                maxRetryDelay: TimeSpan.FromSeconds(10),
//                errorNumbersToAdd: null);
//            sqlOptions.CommandTimeout(60); // Optional: Increases command timeout
//        },
//        c=>c.UseNetTopologySuite())
//    );

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

//builder.Services.AddSingleton(builder.Configuration.GetSection("IdentityPass").Get<IdentitypassOptions>());
builder.Services.Configure<IdentitypassOptions>(
    builder.Configuration.GetSection("IdentityPass"));

//builder.Services.AddSingleton(builder.Configuration.GetSection("AesOptions").Get<AesOptions>());
builder.Services.Configure<AesOptions>(
    builder.Configuration.GetSection("AesOptions"));

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<BulkUploadBgService>();
builder.Services.AddHostedService<FarmerOrganizationSyncService>();
// Register the bulk onboarding processor so scoped resolution from background tasks works
builder.Services.AddScoped<BulkOnboardingProcessor>();


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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        )
    };
});
builder.Services.Configure<FormOptions>(o =>
{
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartBodyLengthLimit = int.MaxValue;
    o.MemoryBufferThreshold = int.MaxValue;
});


builder.Services.AddControllers();

builder.Services.AddHttpClient();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DataContext>("Database", tags: new[] { "database", "critical" })
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "SQL Server Database",
        healthQuery: "SELECT 1",
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "database", "critical" })
    
    .AddCheck<IdentityPassHealthCheck>("IdentityPass API", tags: new[] { "external" })
    .AddCheck<TermiiSmsHealthCheck>("Termii SMS API", tags: new[] { "external" })
    .AddCheck<CountryStateCityApiHealthCheck>("Country State City API", tags: new[] { "external" })
    .AddCheck<SendGridHealthCheck>("SendGrid API", tags: new[] { "external" })
    .AddS3(options =>
    {

        options.BucketName = builder.Configuration["AWS:BucketName"];
        options.AccessKey = builder.Configuration["AWS:AccessKey"];
        options.SecretKey = builder.Configuration["AWS:SecretKey"];
       // options.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(builder.Configuration["AWS:Region"]);
    }, name: "AWS S3", tags: new[] { "external", "storage" })
    
    .AddCheck<ApiEndpointHealthCheck>("API Endpoints", tags: new[] { "api", "endpoints" })
    .AddCheck<ApplicationComponentHealthCheck>("Application Components", tags: new[] { "components", "critical" });

// Register health check classes
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
    //option.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    //{
    //    Description = "Enter your API Key",
    //    Type = SecuritySchemeType.ApiKey,
    //    Name = "x-api-key",
    //    In = ParameterLocation.Header
    //});
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
        //,
        //{
        //    new OpenApiSecurityScheme
        //    {
        //        Reference = new OpenApiReference
        //        {
        //            Type = ReferenceType.SecurityScheme,
        //            Id = "ApiKey"
        //        }
        //    },
        //    new string[] {}
        //}
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

builder.Services.AddHealthChecks();

//var corsOrigins = builder.Configuration.GetValue<string>("CorsOrigins").Split(",");
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
// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();

    //app.UseWhen(
    //ctx => ctx.Request.Path.StartsWithSegments("/swagger/v1/swagger.json"),
    //swaggerApp =>
    //{
    //    swaggerApp.Use(async (context, next) =>
    //    {
    //        var allowedKey = "nomaos123456789t";
    //        var providedKey = context.Request.Headers["x-api-key"];

    //        if (providedKey != allowedKey)
    //        {
    //            context.Response.StatusCode = 403;
    //            await context.Response.WriteAsync("Forbidden");
    //            return;
    //        }

    //        await next();
    //    });
    //});
   
    
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NomaOS ");
        c.DocumentTitle = "NOMAOS Platform API";
        //  c.RoutePrefix = "/docs";

    });
}


app.UseCors();
app.UseRouting();
app.UseHttpsRedirection();

// Enable static files for health dashboard
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// The health check endpoints are now handled by the HealthController
// No additional mapping needed as we're using controller routes

app.MapControllers();

app.Run();
