using System;
using System.Collections.Generic;

namespace Foundatio.Skeleton.Core.Queues.Models
{
    public class MailMessage
    {
        public MailMessage()
        {
            To = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Cc = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Bcc = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public ISet<string> To { get; set; }

        public ISet<string> Cc { get; set; }

        public ISet<string> Bcc { get; set; }

        public string From { get; set; }

        public string Subject { get; set; }

        public string TextBody { get; set; }

        public string HtmlBody { get; set; }
    }
}
