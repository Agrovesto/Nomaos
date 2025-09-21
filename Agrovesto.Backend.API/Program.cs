using Agrovesto.Backend.API.Extensions;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Rekognition;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.API.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using Amazon;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddDbContext<DataContext>(options =>

//              options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
//                  b => b.MigrationsAssembly("Agrovesto.Backend.API"))

//              );

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
            sqlOptions.CommandTimeout(60); // Optional: Increases command timeout
        }));


builder.Services.AddSingleton(builder.Configuration.GetSection("IdentityPass").Get<IdentitypassOptions>());

builder.Services.AddSingleton(builder.Configuration.GetSection("AesOptions").Get<AesOptions>());


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

// Add health checks
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks()
    // Database health checks
    .AddDbContextCheck<DataContext>("Database", tags: new[] { "database", "critical" })
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "SQL Server Database",
        healthQuery: "SELECT 1",
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "database", "critical" })
    
    // External service health checks
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
    
    // API endpoint health checks
    .AddCheck<ApiEndpointHealthCheck>("API Endpoints", tags: new[] { "api", "endpoints" })
    .AddCheck<ApplicationComponentHealthCheck>("Application Components", tags: new[] { "components", "critical" });

// Register health check classes
builder.Services.AddScoped<IdentityPassHealthCheck>();
builder.Services.AddScoped<TermiiSmsHealthCheck>();
builder.Services.AddScoped<CountryStateCityApiHealthCheck>();
builder.Services.AddScoped<SendGridHealthCheck>();
builder.Services.AddScoped<ApiEndpointHealthCheck>();
builder.Services.AddScoped<ApplicationComponentHealthCheck>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Agrovesto Enterprise Platform", Version = "v1" });
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

   // option.OperationFilter<AddCustomHeaderParameter>();
});
// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Register your service
builder.Services.AddHealthChecks();

var corsOrigins = builder.Configuration.GetValue<string>("CorsOrigins").Split(",");
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NomaOS ");
    c.DocumentTitle = "Agrovesto Enterprise Platform";
    //  c.RoutePrefix = "/docs";

});
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var logger = services.GetRequiredService<ILogger<Program>>();

//    try
//    {
//        //await DbInitializer.SeedRolesAndSuperAdminAsync(services);
//        await DbInitializer.SeedWeight(services);
//    }
//    catch (Exception ex)
//    {
//        logger.LogError($"Database seeding encountered an error but will not stop application: {ex.Message}");
//    }
//}
//using (var scope = app.Services.CreateScope())
//{
//    var importer = scope.ServiceProvider.GetRequiredService<INigeriaStateImporterService>();
//    await importer.ImportLocationsFromJsonAsync();
//}
//using (var scope = app.Services.CreateScope())
//{
//    var importer = scope.ServiceProvider.GetRequiredService<IFarmerCategoryService>();
//    await importer.ImportFarmerValuesFromJsonAsync();
//}
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
//    db.Database.Migrate();
//    await SeedData.EnsureSeededAsync(db);
//}



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
