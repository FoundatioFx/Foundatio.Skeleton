using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundatio.Logging;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.Mail;
using MailMessage = Foundatio.Skeleton.Core.Queues.Models.MailMessage;

namespace Foundatio.Skeleton.Domain.Mail
{
    public class WhitelistMailSender : IMailSender
    {
        private readonly ILogger _logger;


        public WhitelistMailSender(ILoggerFactory loggerFactory, IMailSender innerSender)
        {
            if (innerSender == null)
                throw new ArgumentNullException("innerSender");

            _logger = loggerFactory?.CreateLogger<WhitelistMailSender>() ?? NullLogger.Instance;

            InnerSender = innerSender;
        }

        public IMailSender InnerSender { get; private set; }

        public Task SendAsync(MailMessage model)
        {
            // clean before sending
            CleanAddresses(model);

            // send to wrapped sender
            return InnerSender.SendAsync(model);
        }

        private void CleanAddresses(MailMessage message)
        {
            if (Settings.Current.AppMode == AppMode.Production)
                return;

            var invalid = new List<string>();
            invalid.AddRange(CleanAddresses(message.To));
            invalid.AddRange(CleanAddresses(message.Cc));
            invalid.AddRange(CleanAddresses(message.Bcc));

            if (invalid.Count == 0)
                return;

            var invalidAddresses = invalid.ToDelimitedString();
            if (invalid.Count <= 3)
                message.Subject = String.Concat("[", invalidAddresses, "] ", message.Subject).StripInvisible();

            var testAddress = Settings.Current.TestEmailAddress;
            message.To.Add(testAddress);

            _logger.Info("Redirect Email to {0};  Original Recipients: {1}", testAddress, invalidAddresses);
        }

        private static IEnumerable<string> CleanAddresses(ISet<string> addresses)
        {
            var allowed = Settings.Current.AllowedOutboundAddresses;
            var invalid = addresses.Where(address => !allowed.Any(address.Contains)).ToList();

            // remove invalid address
            invalid.ForEach(a => addresses.Remove(a));

            return invalid;
        }

    }
}
