using System;
using System.Collections.Generic;
using System.Linq;
using Foundatio.Repositories.Queries;

namespace Foundatio.Skeleton.Domain.Repositories {
    public interface IOrganizationIdQuery: IRepositoryQuery
    {
        List<string> OrganizationIds { get; }
    }

    public static class OrganizationIdQueryExtensions {
        public static T WithOrganizationId<T>(this T query, string id) where T : IOrganizationIdQuery {
            if (!String.IsNullOrEmpty(id))
                query.OrganizationIds.Add(id);
            return query;
        }

        public static T WithOrganizationIds<T>(this T query, params string[] ids) where T : IOrganizationIdQuery {
            query.OrganizationIds.AddRange(ids.Distinct());
            return query;
        }

        public static T WithOrganizationIds<T>(this T query, IEnumerable<string> ids) where T : IOrganizationIdQuery {
            query.OrganizationIds.AddRange(ids.Distinct());
            return query;
        }
    }
}
