using System;
using GoodProspect.Domain.Models;

namespace GoodProspect.Core.Mail.Models
{
    public class AddedToOrganizationModel : MailModelBase {
        public User Sender { get; set; }
        public Organization Organization { get; set; }
        public User User { get; set; }
    }
}