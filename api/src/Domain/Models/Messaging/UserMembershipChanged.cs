using System;
using Foundatio.Repositories.Models;

namespace Foundatio.Skeleton.Domain.Models.Messaging
{
    public class UserMembershipChanged {
        public ChangeType ChangeType { get; set; }
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
    }
}
