using FluentValidation;
using Foundatio.Caching;
using Foundatio.Extensions;
using Foundatio.Lock;
using Foundatio.Messaging;
using Foundatio.Queues;
using Foundatio.Resilience;
using Foundatio.Serializer;
using Foundatio.Skeleton.Core.Configuration;
using Foundatio.Skeleton.Core.Health;
using Foundatio.Skeleton.Core.Mail;
using Foundatio.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Foundatio.Skeleton.Core;

public static class Bootstrapper
{
    public static void RegisterServices(IServiceCollection services, AppOptions appOptions)
    {
        services.AddSingleton<ISerializer>(s => s.GetRequiredService<ITextSerializer>());
        services.AddSingleton<ITextSerializer>(s => new SystemTextJsonSerializer());

        services.ReplaceSingleton<TimeProvider>(_ => TimeProvider.System);
        services.AddSingleton<IResiliencePolicyProvider, ResiliencePolicyProvider>();

        services.AddSingleton<ICacheClient>(s => new InMemoryCacheClient(new InMemoryCacheClientOptions
        {
            CloneValues = true,
            Serializer = s.GetRequiredService<ISerializer>(),
            TimeProvider = s.GetRequiredService<TimeProvider>(),
            ResiliencePolicyProvider = s.GetRequiredService<IResiliencePolicyProvider>(),
            LoggerFactory = s.GetRequiredService<ILoggerFactory>()
        }));

        services.AddSingleton<IMessageBus>(s => new InMemoryMessageBus(new InMemoryMessageBusOptions
        {
            Serializer = s.GetRequiredService<ISerializer>(),
            TimeProvider = s.GetRequiredService<TimeProvider>(),
            ResiliencePolicyProvider = s.GetRequiredService<IResiliencePolicyProvider>(),
            LoggerFactory = s.GetRequiredService<ILoggerFactory>()
        }));
        services.AddSingleton<IMessagePublisher>(s => s.GetRequiredService<IMessageBus>());
        services.AddSingleton<IMessageSubscriber>(s => s.GetRequiredService<IMessageBus>());

        services.AddSingleton(s => CreateQueue<MailMessage>(s));
        services.AddSingleton(s => CreateQueue<Jobs.WorkItemData>(s, TimeSpan.FromHours(1)));

        services.AddSingleton<IFileStorage>(s => new InMemoryFileStorage(new InMemoryFileStorageOptions
        {
            Serializer = s.GetRequiredService<ITextSerializer>(),
            TimeProvider = s.GetRequiredService<TimeProvider>(),
            ResiliencePolicyProvider = s.GetRequiredService<IResiliencePolicyProvider>(),
            LoggerFactory = s.GetRequiredService<ILoggerFactory>()
        }));

        services.AddSingleton<CacheLockProvider>(s => new CacheLockProvider(
            s.GetRequiredService<ICacheClient>(),
            s.GetRequiredService<IMessageBus>(),
            s.GetRequiredService<TimeProvider>(),
            s.GetRequiredService<IResiliencePolicyProvider>(),
            s.GetRequiredService<ILoggerFactory>()));
        services.AddSingleton<ILockProvider>(s => s.GetRequiredService<CacheLockProvider>());

        services.AddSingleton<IMailSender, InMemoryMailSender>();

        services.AddValidatorsFromAssemblyContaining<Validation.UserValidator>(ServiceLifetime.Singleton);

        services.AddHealthChecks()
            .AddCheck<CacheHealthCheck>("Cache")
            .AddCheck<MessageBusHealthCheck>("MessageBus")
            .AddCheck<StorageHealthCheck>("Storage");
    }

    public static void LogConfiguration(IServiceProvider serviceProvider, AppOptions appOptions, ILogger logger)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        if (String.IsNullOrEmpty(appOptions.CacheOptions.Provider))
            logger.LogWarning("Distributed cache is NOT enabled on {MachineName}", Environment.MachineName);

        if (String.IsNullOrEmpty(appOptions.MessageBusOptions.Provider))
            logger.LogWarning("Distributed message bus is NOT enabled on {MachineName}", Environment.MachineName);

        if (String.IsNullOrEmpty(appOptions.QueueOptions.Provider))
            logger.LogWarning("Distributed queue is NOT enabled on {MachineName}", Environment.MachineName);

        if (String.IsNullOrEmpty(appOptions.StorageOptions.Provider))
            logger.LogWarning("Distributed storage is NOT enabled on {MachineName}", Environment.MachineName);

        if (String.IsNullOrEmpty(appOptions.EmailOptions.SmtpHost))
            logger.LogWarning("Emails will NOT be sent until SmtpHost is configured on {MachineName}", Environment.MachineName);
    }

    private static IQueue<T> CreateQueue<T>(IServiceProvider container, TimeSpan? workItemTimeout = null) where T : class
    {
        return new InMemoryQueue<T>(new InMemoryQueueOptions<T>
        {
            WorkItemTimeout = workItemTimeout.GetValueOrDefault(TimeSpan.FromMinutes(5.0)),
            Serializer = container.GetRequiredService<ISerializer>(),
            TimeProvider = container.GetRequiredService<TimeProvider>(),
            ResiliencePolicyProvider = container.GetRequiredService<IResiliencePolicyProvider>(),
            LoggerFactory = container.GetRequiredService<ILoggerFactory>()
        });
    }
}
