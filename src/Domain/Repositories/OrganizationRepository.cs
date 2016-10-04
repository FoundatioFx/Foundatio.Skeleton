using System;
using System.Linq;
using System.Threading.Tasks;
using Foundatio.Repositories.Elasticsearch;
using Foundatio.Repositories.Elasticsearch.Queries.Builders;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories.Configuration;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Domain.Repositories {
    public class OrganizationRepository : ElasticRepositoryBase<Organization>, IOrganizationRepository {
        public OrganizationRepository(AppElasticConfiguration configuration)
            : base(configuration.Organizations.OrganizationType) {}

        public async Task<Tuple<Organization, Invite>> GetByInviteTokenAsync(string token) {
            if (String.IsNullOrEmpty(token))
                return null;

            var organization = (await FindOneAsync(new CrmQuery().WithFieldEquals(OrganizationType.Fields.InviteToken, token)).AnyContext())?.Document;
            Invite invite = null;
            if (organization != null)
                invite = organization.Invites.FirstOrDefault(i => String.Equals(i.Token, token, StringComparison.OrdinalIgnoreCase));

            return Tuple.Create(organization, invite);
        }
    }
}
