using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.API.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using Amazon;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Register HttpClient for external API health checks
builder.Services.AddHttpClient();

// Register custom health check classes
builder.Services.AddScoped<IdentityPassHealthCheck>();
builder.Services.AddScoped<TermiiSmsHealthCheck>();
builder.Services.AddScoped<CountryStateCityApiHealthCheck>();
builder.Services.AddScoped<SendGridHealthCheck>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DataContext>("Database", tags: new[] { "ready", "live" })
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "SQL Server Database",
        tags: new[] { "ready" })
    .AddCheck<IdentityPassHealthCheck>("IdentityPass API", tags: new[] { "ready" })
    .AddCheck<TermiiSmsHealthCheck>("Termii SMS API", tags: new[] { "ready" })
    .AddCheck<CountryStateCityApiHealthCheck>("Country State City API", tags: new[] { "ready" })
    .AddCheck<SendGridHealthCheck>("SendGrid API", tags: new[] { "ready" })
    .AddS3(options =>
    {
        options.BucketName = builder.Configuration["AWS:BucketName"];
        options.AccessKey = builder.Configuration["AWS:AccessKey"];
        options.SecretKey = builder.Configuration["AWS:SecretKey"];
        options.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(builder.Configuration["AWS:Region"]);
    }, name: "AWS S3", tags: new[] { "ready" });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.

    app.UseSwagger();
    app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseAuthorization();

// Map Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions() {
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        var result = JsonSerializer.Serialize(
            new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    responseTime = e.Value.Duration.TotalMilliseconds,
                    status = e.Value.Status.ToString(),
                    e.Value.Description
                })
            });
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/live", new HealthCheckOptions() {
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        var result = JsonSerializer.Serialize(
            new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    responseTime = e.Value.Duration.TotalMilliseconds,
                    status = e.Value.Status.ToString(),
                    e.Value.Description
                })
            });
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }
});

app.MapControllers();

app.Run();
