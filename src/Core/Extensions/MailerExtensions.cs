using System;
using System.IO;
using System.Net.Mail;

namespace Foundatio.Skeleton.Core.Extensions {
    public static class MailerExtensions {
        public static Queues.Models.MailMessage ToMailMessage(this System.Net.Mail.MailMessage message) {
            var notification = new Queues.Models.MailMessage();
            notification.From = message.From != null ? message.From.ToString() : null;
            notification.Subject = message.Subject;

            foreach (var address in message.To)
                notification.To.Add(address.Address);

            foreach (var address in message.CC)
                notification.Cc.Add(address.Address);

            foreach (var address in message.Bcc)
                notification.Bcc.Add(address.Address);

            if (message.AlternateViews.Count == 0)
                throw new ArgumentException("MailMessage must contain an alternative view.", "message");

            foreach (AlternateView view in message.AlternateViews) {
                if (view.ContentType.MediaType == "text/html")
                    using (var reader = new StreamReader(view.ContentStream))
                        notification.HtmlBody = reader.ReadToEnd();

                if (view.ContentType.MediaType == "text/plain")
                    using (var reader = new StreamReader(view.ContentStream))
                        notification.TextBody = reader.ReadToEnd();

            }

            return notification;
        }

        public static System.Net.Mail.MailMessage ToMailMessage(this Queues.Models.MailMessage notification) {
            var message = new System.Net.Mail.MailMessage();
            message.Subject = notification.Subject;

            foreach (var address in notification.To)
                message.To.Add(address);

            foreach (var address in notification.Cc)
                message.CC.Add(address);

            foreach (var address in notification.Bcc)
                message.Bcc.Add(address);

            if (!String.IsNullOrEmpty(notification.From))
                message.From = new MailAddress(notification.From);

            if (!String.IsNullOrEmpty(notification.TextBody))
                message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(notification.TextBody, null, "text/plain"));

            if (!String.IsNullOrEmpty(notification.HtmlBody))
                message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(notification.HtmlBody, null, "text/html"));

            return message;
        }
    }
}
