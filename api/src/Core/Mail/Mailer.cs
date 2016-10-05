using System;
using System.Collections.Generic;
using System.Net.Mail;
using GoodProspect.Core.Metrics;
using GoodProspect.Core.Extensions;
using GoodProspect.Core.Mail.Models;
using GoodProspect.Domain.Models;
using Foundatio.Metrics;
using Foundatio.Queues;
using RazorSharpEmail;
using MailMessage = GoodProspect.Core.Queues.Models.MailMessage;

namespace GoodProspect.Core.Mail {
    public class Mailer : IMailer {
        private readonly IEmailGenerator _emailGenerator;
        private readonly IQueue<MailMessage> _queue;
        private readonly IMetricsClient _statsClient;

        public Mailer(IEmailGenerator emailGenerator, IQueue<MailMessage> queue, IMetricsClient statsClient) {
            _emailGenerator = emailGenerator;
            _queue = queue;
            _statsClient = statsClient;
        }

        public void SendPasswordReset(User user) {
            if (user == null || String.IsNullOrEmpty(user.PasswordResetToken))
                return;

            System.Net.Mail.MailMessage msg = _emailGenerator.GenerateMessage(new UserModel {
                User = user,
                BaseUrl = Settings.Current.BaseURL
            }, "PasswordReset");
            msg.To.Add(user.EmailAddress);
            QueueMessage(msg);
        }

        public void SendVerifyEmail(User user) {
            System.Net.Mail.MailMessage msg = _emailGenerator.GenerateMessage(new UserModel {
                User = user,
                BaseUrl = Settings.Current.BaseURL
            }, "VerifyEmail");
            msg.To.Add(user.EmailAddress);
            QueueMessage(msg);
        }

        public void SendInvite(User sender, Organization organization, Invite invite) {
            System.Net.Mail.MailMessage msg = _emailGenerator.GenerateMessage(new InviteModel {
                Sender = sender,
                Organization = organization,
                Invite = invite,
                BaseUrl = Settings.Current.BaseURL
            }, "Invite");
            msg.To.Add(invite.EmailAddress);
            QueueMessage(msg);
        }

        public void SendPaymentFailed(User owner, Organization organization) {
            System.Net.Mail.MailMessage msg = _emailGenerator.GenerateMessage(new PaymentModel {
                Owner = owner,
                Organization = organization,
                BaseUrl = Settings.Current.BaseURL
            }, "PaymentFailed");
            msg.To.Add(owner.EmailAddress);
            QueueMessage(msg);
        }

        public void SendAddedToOrganization(User sender, Organization organization, User user) {
            System.Net.Mail.MailMessage msg = _emailGenerator.GenerateMessage(new AddedToOrganizationModel {
                Sender = sender,
                Organization = organization,
                User = user,
                BaseUrl = Settings.Current.BaseURL
            }, "AddedToOrganization");
            msg.To.Add(user.EmailAddress);

            QueueMessage(msg);
        }

        private void QueueMessage(System.Net.Mail.MailMessage message) {
            CleanAddresses(message);

            _queue.Enqueue(message.ToMailMessage());
            _statsClient.Counter(MetricNames.EmailsQueued);
        }

        private static void CleanAddresses(System.Net.Mail.MailMessage msg) {
            if (Settings.Current.WebsiteMode == WebsiteMode.Production)
                return;

            var invalid = new List<string>();
            invalid.AddRange(CleanAddresses(msg.To));
            invalid.AddRange(CleanAddresses(msg.CC));
            invalid.AddRange(CleanAddresses(msg.Bcc));

            if (invalid.Count == 0)
                return;

            if (invalid.Count <= 3)
                msg.Subject = String.Concat("[", invalid.ToDelimitedString(), "] ", msg.Subject).StripInvisible();

            msg.To.Add(Settings.Current.TestEmailAddress);
        }

        private static IEnumerable<string> CleanAddresses(MailAddressCollection mac) {
            var invalid = new List<string>();
            for (int i = 0; i < mac.Count; i++) {
                if (!Settings.Current.AllowedOutboundAddresses.Contains(v => mac[i].Address.ToLowerInvariant().Contains(v))) {
                    invalid.Add(mac[i].Address);
                    mac.RemoveAt(i);
                    i--;
                }
            }

            return invalid;
        }
    }
}