using System;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Api.Models
{
    public class NewNotification : IOwnedByOrganization
    {
        public string OrganizationId { get; set; }
        public string UserId { get; set; }

        public string Type { get; set; }
        public string Message { get; set; }

    }
}