using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Core.Mail
{
    public class SmtpMailSender : IMailSender
    {
        private long _messagesSent;

        public long SentCount { get { return _messagesSent; } }

        public async Task SendAsync(Queues.Models.MailMessage model)
        {
            var client = new SmtpClient();
            var message = model.ToMailMessage();
            message.Headers.Add("X-Mailer-Machine", Environment.MachineName);
            message.Headers.Add("X-Mailer-Date", DateTime.Now.ToString());

            await client.SendMailAsync(message);

            Interlocked.Increment(ref _messagesSent);
        }
    }
}
