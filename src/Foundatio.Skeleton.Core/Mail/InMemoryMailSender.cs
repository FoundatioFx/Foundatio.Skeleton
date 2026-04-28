using Microsoft.Extensions.Logging;

namespace Foundatio.Skeleton.Core.Mail;

public interface IMailSender
{
    Task SendAsync(MailMessage message, CancellationToken cancellationToken = default);
}

public class InMemoryMailSender : IMailSender
{
    private readonly ILogger<InMemoryMailSender> _logger;

    public InMemoryMailSender(ILogger<InMemoryMailSender> logger) => _logger = logger;

    public List<MailMessage> SentMessages { get; } = [];

    public Task SendAsync(MailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mail sent to {To}: {Subject}", message.To, message.Subject);
        SentMessages.Add(message);
        return Task.CompletedTask;
    }
}
