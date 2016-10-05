using System;
using Foundatio.Repositories.Models;

namespace Foundatio.Skeleton.Domain.Models
{
    public interface IOwnedByOrganizationWithIdentity : IOwnedByOrganization, IIdentity
    {
    }
}