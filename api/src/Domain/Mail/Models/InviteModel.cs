using System;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Mail.Models
{
    public class InviteModel : MailModelBase {
        public User Sender { get; set; }
        public Organization Organization { get; set; }
        public Invite Invite { get; set; }
    }
}
