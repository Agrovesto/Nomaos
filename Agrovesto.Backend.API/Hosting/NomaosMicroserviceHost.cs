using Agrovesto.Backend.API.Controllers;
using Agrovesto.Backend.API.Extensions;
using Agrovesto.Backend.Infrastructure;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.Outbox;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Amazon;
using Amazon.SimpleNotificationService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Agrovesto.Backend.API.Hosting;

public static class NomaosMicroserviceHost
{
    public static WebApplication Build(
        string[] args,
        string serviceName,
        IEnumerable<string> controllerTypeNames,
        IReadOnlyDictionary<string, ISet<string>>? actionAllowLists = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddEnvironmentVariables(prefix: "NOMAOS_");

        var allowedControllers = controllerTypeNames.ToHashSet(StringComparer.Ordinal);
        builder.Services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(NomaosMicroserviceHost).Assembly.GetName().Name);
                    sqlOptions.UseNetTopologySuite();
                }));

        builder.Services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<DataContext>()
            .AddDefaultTokenProviders();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                            ?? throw new InvalidOperationException("Jwt:Key must be configured."))),
                    RoleClaimType = "Roles"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var tokenId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                        if (string.IsNullOrEmpty(tokenId))
                        {
                            context.Fail("Token missing jti claim.");
                            return;
                        }

                        var onboardingRepository = context.HttpContext.RequestServices
                            .GetRequiredService<Agrovesto.Backend.Infrastructure.Logic.Repositories.V2.IOnboardingRepository>();
                        if (await onboardingRepository.IsRevokedAsync(tokenId))
                        {
                            context.Fail("Token has been revoked.");
                        }
                    }
                };
            });

        builder.Services.AddAppServices(builder.Configuration);
        builder.Services.RemoveAll<IHostedService>();
        builder.Services.Configure<OutboxOptions>(options => options.Source = serviceName);
        builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
        builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
        builder.Services.AddHostedService<SnsOutboxDispatcher>();
        builder.Services.AddHttpClient();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<DataContext>("database", tags: ["database", "critical"]);

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(BaseController).Assembly)
            .ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Add(new AllowedControllerFeatureProvider(allowedControllers));
            })
            .AddMvcOptions(options =>
            {
                if (actionAllowLists is not null)
                {
                    options.Conventions.Add(new AllowedActionConvention(actionAllowLists));
                }
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = $"Nomaos {serviceName} API",
                Version = "v1"
            }));

        var corsOrigins = builder.Configuration.GetValue<string>("CorsOrigins")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (corsOrigins.Length == 0)
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                return;
            }

            policy.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader();
        }));

        var app = builder.Build();
        app.Logger.LogInformation("Starting {ServiceName}", serviceName);
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        app.MapGet("/", () => Results.Ok(new { service = serviceName, status = "healthy" }))
            .AllowAnonymous();
        app.MapControllers();

        return app;
    }

    private sealed class AllowedControllerFeatureProvider(
        ISet<string> allowedControllers) : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            for (var index = feature.Controllers.Count - 1; index >= 0; index--)
            {
                if (!allowedControllers.Contains(feature.Controllers[index].FullName!))
                {
                    feature.Controllers.RemoveAt(index);
                }
            }
        }
    }

    private sealed class AllowedActionConvention(
        IReadOnlyDictionary<string, ISet<string>> actionAllowLists) : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                if (!actionAllowLists.TryGetValue(controller.ControllerType.FullName!, out var allowedActions))
                {
                    continue;
                }

                for (var index = controller.Actions.Count - 1; index >= 0; index--)
                {
                    if (!allowedActions.Contains(controller.Actions[index].ActionMethod.Name))
                    {
                        controller.Actions.RemoveAt(index);
                    }
                }
            }
        }
    }
}
