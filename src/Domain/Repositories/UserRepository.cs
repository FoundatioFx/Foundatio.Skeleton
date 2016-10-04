using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundatio.Repositories.Elasticsearch;
using Foundatio.Repositories.Elasticsearch.Queries;
using Foundatio.Repositories.Elasticsearch.Queries.Builders;
using Foundatio.Repositories.Models;
using Foundatio.Repositories.Queries;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories.Configuration;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Domain.Repositories {
    public class UserRepository : ElasticRepositoryBase<User>, IUserRepository {
        public UserRepository(AppElasticConfiguration configuration)
            : base(configuration.Organizations.UserType) {
            DocumentsChanging.AddHandler(OnDocumentsChanging);
        }

        private Task OnDocumentsChanging(object sender, DocumentsChangeEventArgs<User> args) {
            if (args.ChangeType == ChangeType.Removed)
                return Task.CompletedTask;

            foreach (var doc in args.Documents) {
                doc.Value.EmailAddress = doc.Value.EmailAddress.ToLower();

                // automatically verify email if its in the oauth accounts
                if (!doc.Value.IsEmailAddressVerified)
                    doc.Value.IsEmailAddressVerified = doc.Value.OAuthAccounts.Count(oa => String.Equals(oa.EmailAddress(), doc.Value.EmailAddress, StringComparison.OrdinalIgnoreCase)) > 0;

                if (!doc.Value.IsEmailAddressVerified && String.IsNullOrEmpty(doc.Value.VerifyEmailAddressToken))
                    doc.Value.CreateVerifyEmailAddressToken();
            }

            return Task.CompletedTask;
        }

        public async Task<User> GetByEmailAddressAsync(string emailAddress) {
            if (String.IsNullOrEmpty(emailAddress))
                return null;

            emailAddress = emailAddress.ToLower();
            return (await FindOneAsync(new CrmQuery().WithFieldEquals(UserType.Fields.EmailAddress, emailAddress).WithCacheKey(emailAddress)).AnyContext())?.Document;
        }

        public async Task<User> GetByPasswordResetTokenAsync(string token) {
            if (String.IsNullOrEmpty(token))
                return null;

            return (await FindOneAsync(new CrmQuery().WithFieldEquals(UserType.Fields.PasswordResetToken, token)).AnyContext())?.Document;
        }

        public async Task<User> GetUserByOAuthProviderAsync(string provider, string providerUserId) {
            if (String.IsNullOrEmpty(provider) || String.IsNullOrEmpty(providerUserId))
                return null;

            provider = provider.ToLowerInvariant();

            var results = (await FindAsync(new CrmQuery()
                .WithFieldEquals(UserType.Fields.OAuthAccountProviderUserId, providerUserId)).AnyContext()).Documents;

            return results.FirstOrDefault(u => u.OAuthAccounts.Any(o => o.Provider == provider));
        }

        public async Task<User> GetByVerifyEmailAddressTokenAsync(string token) {
            if (String.IsNullOrEmpty(token))
                return null;

            return (await FindOneAsync(new CrmQuery().WithFieldEquals(UserType.Fields.VerifyEmailAddressToken, token)).AnyContext())?.Document;
        }

        public virtual Task<FindResults<User>> GetByOrganizationIdAsync(string organizationId, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null) {
            return GetByOrganizationIdsAsync(new[] { organizationId }, paging, useCache, expiresIn);
        }

        public async Task<long> CountByOrganizationIdAsync(string organizationId) {
            var result = await CountAsync(new CrmQuery()
                .WithFieldEquals(UserType.Fields.MembershipOrganizationId, organizationId));

            return result.Total;
        }

        public virtual Task<FindResults<User>> GetByOrganizationIdsAsync(ICollection<string> organizationIds, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null) {
            if (organizationIds == null || organizationIds.Count == 0)
                return Task.FromResult<FindResults<User>>(new FindResults<User>());

            string cacheKey = String.Concat("org:", String.Join("", organizationIds).GetHashCode().ToString());
            return FindAsync(new CrmQuery()
                .WithFieldEquals(UserType.Fields.MembershipOrganizationId, organizationIds)
                .WithPaging(paging)
                .WithCacheKey(useCache ? cacheKey : null)
                .WithExpiresIn(expiresIn));
        }

        protected override async Task InvalidateCacheAsync(IReadOnlyCollection<ModifiedDocument<User>> users) {
            if (!IsCacheEnabled)
                return;

            if (users == null)
                throw new ArgumentNullException(nameof(users));

            var emails = users.Select(u => u.Value.EmailAddress)
                .Union(users.Where(u => u.Original != null).Select(u => u.Original.EmailAddress))
                .Where(e => !e.IsNullOrEmpty()).Distinct();

            await Cache.RemoveAllAsync(emails).AnyContext();

            var organizations = users.SelectMany(u => u.Value.Memberships).Select(u => u.OrganizationId)
                .Union(users.Where(u => u.Original != null).SelectMany(u => u.Original.Memberships).Select(u => u.OrganizationId))
                .Where(e => !e.IsNullOrEmpty()).Distinct();

            await Cache.RemoveAllAsync(organizations
                .Select(orgId => "org:" + orgId)
                .Distinct()).AnyContext();

            await base.InvalidateCacheAsync(users).AnyContext();
        }
    }
}
