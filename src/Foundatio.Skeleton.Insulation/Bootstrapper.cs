using Foundatio.Caching;
using Foundatio.Extensions;
using Foundatio.Messaging;
using Foundatio.Resilience;
using Foundatio.Serializer;
using Foundatio.Skeleton.Core.Configuration;
using Foundatio.Skeleton.Core.Mail;
using Foundatio.Skeleton.Insulation.Mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Foundatio.Skeleton.Insulation;

public static class Bootstrapper
{
    public static void RegisterServices(IServiceCollection services, AppOptions appOptions)
    {
        RegisterCache(services, appOptions.CacheOptions);
        RegisterMessageBus(services, appOptions.MessageBusOptions);

        if (!String.IsNullOrEmpty(appOptions.EmailOptions.SmtpHost))
            services.ReplaceSingleton<IMailSender, MailKitMailSender>();
    }

    private static void RegisterCache(IServiceCollection container, CacheOptions options)
    {
        if (!String.Equals(options.Provider, "redis", StringComparison.OrdinalIgnoreCase))
            return;

        container.ReplaceSingleton(s =>
            (IConnectionMultiplexer)ConnectionMultiplexer.Connect(
                options.ConnectionString!,
                o => o.LoggerFactory = s.GetRequiredService<ILoggerFactory>()));

        if (!String.IsNullOrEmpty(options.Scope))
        {
            container.ReplaceSingleton<ICacheClient>(s =>
                new ScopedCacheClient(CreateRedisCacheClient(s), options.Scope));
        }
        else
        {
            container.ReplaceSingleton<ICacheClient>(CreateRedisCacheClient);
        }
    }

    private static void RegisterMessageBus(IServiceCollection container, MessageBusOptions options)
    {
        if (!String.Equals(options.Provider, "redis", StringComparison.OrdinalIgnoreCase))
            return;

        container.ReplaceSingleton(s =>
            (IConnectionMultiplexer)ConnectionMultiplexer.Connect(
                options.ConnectionString!,
                o => o.LoggerFactory = s.GetRequiredService<ILoggerFactory>()));

        container.ReplaceSingleton<IMessageBus>(s => new RedisMessageBus(new RedisMessageBusOptions
        {
            Subscriber = s.GetRequiredService<IConnectionMultiplexer>().GetSubscriber(),
            Topic = options.Topic!,
            Serializer = s.GetRequiredService<ISerializer>(),
            TimeProvider = s.GetRequiredService<TimeProvider>(),
            ResiliencePolicyProvider = s.GetRequiredService<IResiliencePolicyProvider>(),
            LoggerFactory = s.GetRequiredService<ILoggerFactory>()
        }));
    }

    private static RedisCacheClient CreateRedisCacheClient(IServiceProvider container)
    {
        return new RedisCacheClient(new RedisCacheClientOptions
        {
            ConnectionMultiplexer = container.GetRequiredService<IConnectionMultiplexer>(),
            Serializer = container.GetRequiredService<ISerializer>(),
            TimeProvider = container.GetRequiredService<TimeProvider>(),
            ResiliencePolicyProvider = container.GetRequiredService<IResiliencePolicyProvider>(),
            LoggerFactory = container.GetRequiredService<ILoggerFactory>()
        });
    }
}
