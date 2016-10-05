using System;
using System.Threading.Tasks;
using Foundatio.Jobs;
using Foundatio.Logging;
using Foundatio.Queues;
using Foundatio.Skeleton.Core.Mail;
using Foundatio.Skeleton.Core.Queues.Models;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Core.Jobs {
    public class MailMessageJob : QueueJobBase<MailMessage> {
        private readonly IMailSender _mailSender;

        public MailMessageJob(IQueue<MailMessage> queue, IMailSender mailSender, ILoggerFactory loggerFactory) : base(queue, loggerFactory) {
            _mailSender = mailSender;
            AutoComplete = true;
        }

        protected override async Task<JobResult> ProcessQueueEntryAsync(QueueEntryContext<MailMessage> context) {
            _logger.Trace("Processing message '{0}'.", context.QueueEntry.Id);

            try {
                await _mailSender.SendAsync(context.QueueEntry.Value).ConfigureAwait(false);
                _logger.Info()
                    .Message(() => $"Sent message: to={context.QueueEntry.Value.To.ToDelimitedString()} subject=\"{context.QueueEntry.Value.Subject}\"")
                    .Write();
            } catch (Exception ex) {
                await context.QueueEntry.AbandonAsync().AnyContext();
                return JobResult.FromException(ex);
            }

            return JobResult.Success;
        }
    }
}
