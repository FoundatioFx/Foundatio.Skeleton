using System;
using System.Collections.Generic;
using Foundatio.Repositories.Elasticsearch.Queries;

namespace Foundatio.Skeleton.Domain.Repositories
{
    public sealed class CrmQuery: ElasticQuery, IOrganizationIdQuery
    {
        public List<string> OrganizationIds { get; } = new List<string>();
    }
}
