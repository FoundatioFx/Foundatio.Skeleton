using System;
using System.Threading.Tasks;
using Foundatio.Repositories;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Repositories {
    public interface IOrganizationRepository : ISearchableRepository<Organization> {
        Task<Tuple<Organization, Invite>> GetByInviteTokenAsync(string token);
    }
}
