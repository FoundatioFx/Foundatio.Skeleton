using System;
using System.Threading.Tasks;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Repositories {
    public interface ITokenRepository : IRepositoryOwnedByOrganization<Token> {
        Task<Token> GetByRefreshTokenAsync(string refreshToken);
        Task<FindResults<Token>> GetApiTokensAsync(string organizationId, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null);
        Task<FindResults<Token>> GetByUserIdAsync(string userId);
        Task<Token> GetOrCreateUserToken(string userId, string organizationId);
    }
}
