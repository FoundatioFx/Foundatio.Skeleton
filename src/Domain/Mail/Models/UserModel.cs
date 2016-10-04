using System;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Mail.Models
{
    public class UserModel : MailModelBase {
        public User User { get; set; }
    }
}
