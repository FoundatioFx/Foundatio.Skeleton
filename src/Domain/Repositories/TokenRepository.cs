using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundatio.Repositories.Elasticsearch.Queries;
using Foundatio.Repositories.Elasticsearch.Queries.Builders;
using Foundatio.Repositories.Models;
using Foundatio.Repositories.Queries;
using Foundatio.Skeleton.Domain.Repositories.Configuration;
using Nest;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Domain.Repositories {
    public class TokenRepository : RepositoryOwnedByOrganization<Models.Token>, ITokenRepository {
        public TokenRepository(AppElasticConfiguration configuration)
            : base(configuration.Organizations.TokenType) {
        }

        public Task<FindResults<Models.Token>> GetApiTokensAsync(string organizationId, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null) {
            var filter = Filter<Models.Token>.Term(e => e.Type, Models.TokenType.Access) && Filter<Models.Token>.Missing(e => e.UserId);
            return FindAsync(new CrmQuery()
                .WithOrganizationId(organizationId)
                .WithElasticFilter(filter)
                .WithPaging(paging)
                .WithCacheKey(useCache ? String.Concat("api-org:", organizationId) : null)
                .WithExpiresIn(expiresIn));
        }

        public Task<FindResults<Models.Token>> GetByUserIdAsync(string userId) {
            return FindAsync(new CrmQuery()
                .WithFieldEquals(TokenType.Fields.UserId, userId));
        }

        public async Task<Models.Token> GetOrCreateUserToken(string userId, string organizationId) {
            var existingToken = (await GetByUserIdAsync(userId)).Documents.FirstOrDefault(t => t.OrganizationId == organizationId);
            if (existingToken != null && existingToken.ExpiresUtc > DateTime.UtcNow)
                return existingToken;

            var token = new Models.Token {
                Id = StringUtils.GetNewToken(),
                UserId = userId,
                OrganizationId = organizationId,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                CreatedBy = userId,
                Type = Models.TokenType.Access
            };
            await AddAsync(token);

            return token;
        }

        public async Task<Models.Token> GetByRefreshTokenAsync(string refreshToken) {
            if (String.IsNullOrEmpty(refreshToken))
                throw new ArgumentNullException(nameof(refreshToken));

            return (await FindOneAsync(new CrmQuery().WithFieldEquals(TokenType.Fields.Refresh, refreshToken)).AnyContext())?.Document;
        }

        protected override async Task InvalidateCacheAsync(IReadOnlyCollection<ModifiedDocument<Models.Token>> tokens) {
            if (!IsCacheEnabled)
                return;

            await Cache.RemoveAllAsync(tokens.Select(t => t.Value)
                .Where(t => !String.IsNullOrEmpty(t.OrganizationId))
                .Select(t => String.Concat("type:", t.Type, "-org:", t.OrganizationId))
                .Distinct()).AnyContext();

            await base.InvalidateCacheAsync(tokens);
        }
    }
}
