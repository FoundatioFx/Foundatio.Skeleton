using System;
using System.Threading.Tasks;
using Foundatio.Queues;
using Foundatio.Skeleton.Domain.Mail.Models;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Core.Extensions;
using RazorSharpEmail;
using MailMessage = Foundatio.Skeleton.Core.Queues.Models.MailMessage;

namespace Foundatio.Skeleton.Domain.Services {
    public class TemplatedMailService : ITemplatedMailService {
        private readonly IEmailGenerator _emailGenerator;
        private readonly IQueue<MailMessage> _queue;

        public TemplatedMailService(IEmailGenerator emailGenerator, IQueue<MailMessage> queue) {
            _emailGenerator = emailGenerator;
            _queue = queue;
        }

        public void SendPasswordReset(User user) {
            if (String.IsNullOrEmpty(user?.PasswordResetToken))
                return;

            Task.Run(async () => {
                System.Net.Mail.MailMessage msg = _emailGenerator.GenerateMessage(new UserModel {
                    User = user,
                    BaseUrl = Settings.Current.AppUrl
                }, "PasswordReset");
                msg.To.Add(user.EmailAddress);
                await QueueMessageAsync(msg).AnyContext();
            });
        }

        public void SendPasswordResetEmailAddressNotFound(string emailAddress) {
            var email = new Email { Address = emailAddress };
            var validator = new Validators.EmailValidator();
            if (!validator.TryValidate(email))
                return;

            Task.Run(async () => {
                System.Net.Mail.MailMessage msg = _emailGenerator.GenerateMessage(new PasswordResetEmailAddressNotFoundModel {
                    EmailAddress = emailAddress,
                    BaseUrl = Settings.Current.AppUrl,
                }, "PasswordResetEmailAddressNotFound");
                msg.To.Add(email.Address);
                await QueueMessageAsync(msg).AnyContext();
            });
        }

        public void SendVerifyEmail(User user) {
            Task.Run(async () => {
                System.Net.Mail.MailMessage msg = _emailGenerator.GenerateMessage(new UserModel {
                    User = user,
                    BaseUrl = Settings.Current.AppUrl
                }, "VerifyEmail");
                msg.To.Add(user.EmailAddress);
                await QueueMessageAsync(msg).AnyContext();
            });
        }

        public void SendInvite(User sender, Organization organization, Invite invite) {
            Task.Run(async () => {
                System.Net.Mail.MailMessage msg = _emailGenerator.GenerateMessage(new InviteModel {
                    Sender = sender,
                    Organization = organization,
                    Invite = invite,
                    BaseUrl = Settings.Current.AppUrl
                }, "Invite");
                msg.To.Add(invite.EmailAddress);
                await QueueMessageAsync(msg).AnyContext();
            });
        }

        public void SendAddedToOrganization(User sender, Organization organization, User user) {
            Task.Run(async () => {
                System.Net.Mail.MailMessage msg = _emailGenerator.GenerateMessage(new AddedToOrganizationModel {
                    Sender = sender,
                    Organization = organization,
                    User = user,
                    BaseUrl = Settings.Current.AppUrl
                }, "AddedToOrganization");
                msg.To.Add(user.EmailAddress);

                await QueueMessageAsync(msg).AnyContext();
            });
        }

        private Task QueueMessageAsync(System.Net.Mail.MailMessage message) {
            return _queue.EnqueueAsync(message.ToMailMessage());
        }
    }
}
