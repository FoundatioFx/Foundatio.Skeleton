namespace Foundatio.Skeleton.Core.Mail;

public class MailMessage
{
    public string To { get; set; } = null!;
    public string? From { get; set; }
    public string Subject { get; set; } = null!;
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
}
