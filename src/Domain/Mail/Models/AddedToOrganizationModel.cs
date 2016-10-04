using System;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Mail.Models
{
    public class AddedToOrganizationModel : MailModelBase {
        public User Sender { get; set; }
        public Organization Organization { get; set; }
        public User User { get; set; }
    }
}
