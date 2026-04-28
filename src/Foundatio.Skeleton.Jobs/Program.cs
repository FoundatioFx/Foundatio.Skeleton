using Foundatio.Mediator;
using Foundatio.Skeleton.Core;
using Foundatio.Skeleton.Core.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

var appOptions = builder.Configuration.Get<AppOptions>() ?? new AppOptions();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Foundatio.Skeleton.Jobs"))
    .WithTracing(tracing =>
    {
        tracing.AddHttpClientInstrumentation();
        tracing.AddSource("Foundatio.*");
        tracing.AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddHttpClientInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddMeter("Foundatio.*");
        metrics.AddOtlpExporter();
    });

builder.Services.AddMediator();

Bootstrapper.RegisterServices(builder.Services, appOptions);
Foundatio.Skeleton.Insulation.Bootstrapper.RegisterServices(builder.Services, appOptions);

var host = builder.Build();

Bootstrapper.LogConfiguration(host.Services, appOptions, host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>());

host.Run();
