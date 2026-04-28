using Foundatio.Skeleton.Core.Configuration;
using Foundatio.Skeleton.Core.Mail;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Foundatio.Skeleton.Insulation.Mail;

public class MailKitMailSender : IMailSender
{
    private readonly IOptions<AppOptions> _options;
    private readonly ILogger<MailKitMailSender> _logger;

    public MailKitMailSender(IOptions<AppOptions> options, ILogger<MailKitMailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(Core.Mail.MailMessage message, CancellationToken cancellationToken = default)
    {
        var emailOptions = _options.Value.EmailOptions;
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(MailboxAddress.Parse(message.From ?? emailOptions.DefaultFromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var builder = new BodyBuilder();
        if (!String.IsNullOrEmpty(message.TextBody))
            builder.TextBody = message.TextBody;
        if (!String.IsNullOrEmpty(message.HtmlBody))
            builder.HtmlBody = message.HtmlBody;
        mimeMessage.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(emailOptions.SmtpHost!, emailOptions.SmtpPort, emailOptions.SmtpEnableSsl, cancellationToken);

        if (!String.IsNullOrEmpty(emailOptions.SmtpUser))
            await client.AuthenticateAsync(emailOptions.SmtpUser, emailOptions.SmtpPassword!, cancellationToken);

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Mail sent to {To}: {Subject}", message.To, message.Subject);
    }
}
