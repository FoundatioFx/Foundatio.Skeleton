using System;
using GoodProspect.Domain.Models;

namespace GoodProspect.Core.Mail.Models
{
    public class PaymentModel : MailModelBase {
        public User Owner { get; set; }
        public Organization Organization { get; set; }
    }
}