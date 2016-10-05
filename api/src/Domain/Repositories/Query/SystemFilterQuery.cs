using System;
using System.Collections.Generic;
using Foundatio.Repositories.Queries;

namespace Foundatio.Skeleton.Domain.Repositories.Query
{
    public sealed class SystemFilterQuery: IOrganizationIdQuery, ISoftDeletesQuery
    {
        public List<string> OrganizationIds { get; } = new List<string>();
        public bool IncludeSoftDeletes { get; set; }
    }
}
