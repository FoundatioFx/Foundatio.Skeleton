using System;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Services
{
    public interface ITemplatedMailService
    {
        void SendPasswordReset(User user);
        void SendPasswordResetEmailAddressNotFound(string emailAddress);
        void SendVerifyEmail(User user);
        void SendInvite(User sender, Organization organization, Invite invite);
        void SendAddedToOrganization(User sender, Organization organization, User user);
    }
}
