using Foundatio.Caching;
using Foundatio.Messaging;
using Foundatio.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Foundatio.Skeleton.Core.Health;

public class CacheHealthCheck(ICacheClient cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var key = "health:check";
        await cache.SetAsync(key, "ok", TimeSpan.FromSeconds(10));
        var value = await cache.GetAsync<string>(key);
        return value.HasValue
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cache read/write failed");
    }
}

public class MessageBusHealthCheck(IMessageBus messageBus) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(messageBus is not null
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("MessageBus not available"));
    }
}

public class StorageHealthCheck(IFileStorage storage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var path = "health/check.txt";
        await storage.SaveFileAsync(path, "ok");
        var exists = await storage.ExistsAsync(path);
        await storage.DeleteFileAsync(path);
        return exists
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Storage read/write failed");
    }
}
