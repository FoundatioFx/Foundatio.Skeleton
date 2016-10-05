using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundatio.Repositories;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Repositories {
    public interface IUserRepository : ISearchableRepository<User> {
        Task<User> GetByEmailAddressAsync(string emailAddress);
        Task<User> GetByPasswordResetTokenAsync(string token);
        Task<User> GetUserByOAuthProviderAsync(string provider, string providerUserId);
        Task<User> GetByVerifyEmailAddressTokenAsync(string token);
        Task<long> CountByOrganizationIdAsync(string organizationId);
        Task<FindResults<User>> GetByOrganizationIdAsync(string organizationId, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null);
        Task<FindResults<User>> GetByOrganizationIdsAsync(ICollection<string> organizationIds, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null);
    }
}
