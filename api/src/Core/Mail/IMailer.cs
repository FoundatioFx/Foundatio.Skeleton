using System;
using GoodProspect.Domain.Models;

namespace GoodProspect.Core.Mail
{
    public interface IMailer {
        void SendPasswordReset(User user);
        void SendVerifyEmail(User user);
        void SendInvite(User sender, Organization organization, Invite invite);
        void SendPaymentFailed(User owner, Organization organization);
        void SendAddedToOrganization(User sender, Organization organization, User user);
    }
}