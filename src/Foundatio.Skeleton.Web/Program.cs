using Foundatio.Mediator;
using Foundatio.Skeleton.Core;
using Foundatio.Skeleton.Core.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var appOptions = builder.Configuration.Get<AppOptions>() ?? new AppOptions();
builder.Services.Configure<AppOptions>(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Foundatio.Skeleton"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddProcessInstrumentation();
        metrics.AddOtlpExporter();
        metrics.AddPrometheusExporter();
    });

builder.Services.AddCors(b => b.AddPolicy("AllowAny", p => p
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowed(_ => true)
    .AllowCredentials()
    .SetPreflightMaxAge(TimeSpan.FromMinutes(5))));

builder.Services.AddOpenApi();
builder.Services.AddMediator();

Bootstrapper.RegisterServices(builder.Services, appOptions);
Foundatio.Skeleton.Insulation.Bootstrapper.RegisterServices(builder.Services, appOptions);

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

Bootstrapper.LogConfiguration(app.Services, appOptions, app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>());

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseHealthChecks("/health", new HealthCheckOptions
{
    Predicate = hcr => hcr.Tags.Contains("Critical") || hcr.Tags.Count == 0
});

app.UseCors("AllowAny");

app.MapOpenApi();
app.MapScalarApiReference("/docs", o =>
{
    o.AddDocument("v1", "Foundatio Skeleton API", "/openapi/v1.json", true);
});

app.MapMediatorEndpoints();

app.Run();

// Required for WebApplicationFactory in tests
public partial class Program;
