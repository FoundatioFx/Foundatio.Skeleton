using System;
using GoodProspect.Domain.Models;

namespace GoodProspect.Core.Mail.Models
{
    public class UserModel : MailModelBase {
        public User User { get; set; }
    }
}