using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundatio.Repositories;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Repositories {
    public interface IRepositoryOwnedByOrganization<T> : ISearchableRepository<T> where T : class, IOwnedByOrganization, IIdentity, new() {
        Task<long> CountByOrganizationIdAsync(string organizationId);
        Task<FindResults<T>> GetByOrganizationIdAsync(string organizationId, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null);
        Task<FindResults<T>> GetByOrganizationIdsAsync(ICollection<string> organizationIds, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null);
        Task<long> RemoveAllByOrganizationIdsAsync(string[] organizationIds);
    }
}
