using System;
using Foundatio.Repositories.Elasticsearch.Queries.Builders;
using Nest;

namespace Foundatio.Skeleton.Domain.Repositories {
    public class OrganizationIdQueryBuilder : IElasticQueryBuilder {
        public void Build<T>(QueryBuilderContext<T> ctx) where T : class, new() {
            var organizationIdQuery = ctx.GetSourceAs<IOrganizationIdQuery>();
            if (organizationIdQuery?.OrganizationIds == null || organizationIdQuery.OrganizationIds.Count <= 0)
                return;

            ctx.Filter &= new TermsFilter { Terms = organizationIdQuery.OrganizationIds, Field = "organization" };
        }
    }
}
