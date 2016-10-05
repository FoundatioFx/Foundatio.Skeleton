using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Foundatio.Repositories.Elasticsearch;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Foundatio.Repositories.Elasticsearch.Queries;
using Foundatio.Repositories.Models;
using Foundatio.Repositories.Queries;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Models.Messaging;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Domain.Repositories {
    public abstract class RepositoryOwnedByOrganization<T> : ElasticRepositoryBase<T>, IRepositoryOwnedByOrganization<T> where T : class, IOwnedByOrganization, IIdentity, new() {

        public RepositoryOwnedByOrganization(IIndexType<T> indexType, IValidator<T> validator = null)
            : base(indexType) { }

        public async Task<long> CountByOrganizationIdAsync(string organizationId) {
            var options = new CrmQuery().WithOrganizationId(organizationId);

            var result = await CountAsync(options).AnyContext();
            return result.Total;
        }

        public virtual Task<FindResults<T>> GetByOrganizationIdAsync(string organizationId, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null) {
            return GetByOrganizationIdsAsync(new[] { organizationId }, paging, useCache, expiresIn);
        }

        public virtual async Task<FindResults<T>> GetByOrganizationIdsAsync(ICollection<string> organizationIds, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null) {
            if (organizationIds == null || organizationIds.Count == 0)
                return new FindResults<T>();

            string cacheKey = String.Concat("org:", String.Join("", organizationIds).GetHashCode().ToString());
            return await FindAsync(new CrmQuery()
                .WithOrganizationIds(organizationIds)
                .WithPaging(paging)
                .WithCacheKey(useCache ? cacheKey : null)
                .WithExpiresIn(expiresIn)).AnyContext();
        }

        public Task<long> RemoveAllByOrganizationIdsAsync(string[] organizationIds) {
            return RemoveAllAsync(new CrmQuery().WithOrganizationIds(organizationIds));
        }

        protected override async Task InvalidateCacheAsync(IReadOnlyCollection<ModifiedDocument<T>> documents) {
            if (!IsCacheEnabled)
                return;

            var keys = documents.Select(d => d.Value)
                .Cast<IOwnedByOrganization>()
                .Where(d => !String.IsNullOrEmpty(d.OrganizationId))
                .Select(d => "org:" + d.OrganizationId)
                .Distinct()
                .ToList();

            if (keys.Count > 0) {
                await Cache.RemoveAllAsync(keys).AnyContext();
            }

            await base.InvalidateCacheAsync(documents).AnyContext();
        }

        protected override Task PublishChangeTypeMessageAsync(ChangeType changeType, T document, IDictionary<string, object> data = null, TimeSpan? delay = null) {
            data = data ?? new Dictionary<string, object>();

            data.Add(AppEntityChanged.KnownKeys.OrganizationId, document.OrganizationId);

            return base.PublishChangeTypeMessageAsync(changeType, document, data, delay);
        }
    }
}
