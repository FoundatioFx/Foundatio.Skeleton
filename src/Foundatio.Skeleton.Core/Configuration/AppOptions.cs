namespace Foundatio.Skeleton.Core.Configuration;

public class AppOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public AppMode AppMode { get; set; } = AppMode.Development;

    public CacheOptions CacheOptions { get; set; } = new();
    public MessageBusOptions MessageBusOptions { get; set; } = new();
    public QueueOptions QueueOptions { get; set; } = new();
    public StorageOptions StorageOptions { get; set; } = new();
    public EmailOptions EmailOptions { get; set; } = new();
}

public enum AppMode
{
    Development,
    Staging,
    Production
}

public class CacheOptions
{
    public string? Provider { get; set; }
    public string? ConnectionString { get; set; }
    public string? Scope { get; set; }
}

public class MessageBusOptions
{
    public string? Provider { get; set; }
    public string? ConnectionString { get; set; }
    public string? Topic { get; set; } = "foundatio-skeleton";
}

public class QueueOptions
{
    public string? Provider { get; set; }
    public string? ConnectionString { get; set; }
    public string? ScopePrefix { get; set; }
}

public class StorageOptions
{
    public string? Provider { get; set; }
    public string? ConnectionString { get; set; }
    public string? ScopePrefix { get; set; }
}

public class EmailOptions
{
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpEnableSsl { get; set; } = true;
    public string DefaultFromAddress { get; set; } = "noreply@localhost";
}
