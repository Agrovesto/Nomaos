using Agrovesto.Backend.API.Extensions;
using Agrovesto.Backend.API.HealthChecks;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.Utilities;
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
using Serilog;
using System.Text;
using System.Text.Json;

// =====================
// SERILOG — configure before anything else so startup errors are captured
// =====================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "/var/log/Nomaos/app.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true)
    .CreateLogger();

try
{
    Log.Information("🚀 NOMAOS BOOTSTRAP STARTING");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // =====================
    // DATABASE
    // =====================
    builder.Services.AddDbContext<DataContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql =>
            {
                sql.MigrationsAssembly("Agrovesto.Backend.API");
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                sql.CommandTimeout(300);
                sql.UseNetTopologySuite();
            }));

    // =====================
    // CACHE
    // =====================
    builder.Services.AddStackExchangeRedisCache(options =>
        options.Configuration = builder.Configuration["Redis:ConnectionString"]);

    // =====================
    // IDENTITY
    // =====================
    builder.Services.AddIdentity<User, Role>(opt =>
    {
        opt.SignIn.RequireConfirmedAccount = true;
        opt.Password.RequireDigit = false;
        opt.Password.RequiredLength = 4;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();

    // =====================
    // AUTHENTICATION / JWT
    // =====================
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            };
        });

    // =====================
    // AWS
    // =====================
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonS3>();
    builder.Services.AddAWSService<IAmazonRekognition>();

    // =====================
    // OPTIONS / SINGLETONS
    // =====================
    builder.Services.AddSingleton(builder.Configuration.GetSection("IdentityPass").Get<IdentitypassOptions>());
    builder.Services.AddSingleton(builder.Configuration.GetSection("AesOptions").Get<AesOptions>());
    builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

    // =====================
    // BACKGROUND SERVICES
    // =====================
    builder.Services.AddHostedService<BulkUploadBgService>();
    builder.Services.AddHostedService<FarmerOrganizationSyncService>();
    builder.Services.AddScoped<BulkOnboardingProcessor>();

    // =====================
    // MULTIPART / FORM OPTIONS
    // =====================
    builder.Services.Configure<FormOptions>(options =>
    {
        options.ValueLengthLimit = int.MaxValue;
        options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
        options.MemoryBufferThreshold = int.MaxValue;
    });

    // =====================
    // CORS
    // =====================
    var corsOrigins = builder.Configuration.GetValue<string>("CorsOrigins")!.Split(",");
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .SetIsOriginAllowedToAllowWildcardSubdomains()));

    // =====================
    // FORWARDED HEADERS (must be set before Build for known networks)
    // =====================
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // =====================
    // HEALTH CHECKS
    // =====================
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
        }, name: "AWS S3", tags: new[] { "external", "storage" })
        .AddCheck<ApiEndpointHealthCheck>("API Endpoints", tags: new[] { "api", "endpoints" })
        .AddCheck<ApplicationComponentHealthCheck>("Application Components", tags: new[] { "components", "critical" });

    builder.Services.AddScoped<IdentityPassHealthCheck>();
    builder.Services.AddScoped<TermiiSmsHealthCheck>();
    builder.Services.AddScoped<CountryStateCityApiHealthCheck>();
    builder.Services.AddScoped<SendGridHealthCheck>();
    builder.Services.AddScoped<ApiEndpointHealthCheck>();
    builder.Services.AddScoped<ApplicationComponentHealthCheck>();

    // =====================
    // SWAGGER
    // =====================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(option =>
    {
        option.SwaggerDoc("v1", new OpenApiInfo { Title = "NOMAOS Platform API", Version = "v1" });
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
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // =====================
    // APP SERVICES (extension method)
    // =====================
    builder.Services.AddAppServices(builder.Configuration);
    builder.Services.AddControllers();
    builder.Services.AddHttpClient();

    // =====================
    // BUILD
    // =====================
    var app = builder.Build();

    AppSettingsHelper.AppSettingConfigure(app.Services.GetRequiredService<IConfiguration>());

    // =====================
    // MIDDLEWARE PIPELINE  (order matters)
    // =====================
    app.UseForwardedHeaders();
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "NomaOS");
            c.DocumentTitle = "NOMAOS Platform API";
        });
    }

    app.UseCors();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}