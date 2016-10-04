using System;
using Nest;

namespace Foundatio.Skeleton.Domain.Extensions {
    public static class ElasticSearchIndexExtensions {
        public static ObjectMappingDescriptor<TParent, TChild> RootPath<TParent, TChild>(this ObjectMappingDescriptor<TParent, TChild> t)
            where TParent : class
            where TChild : class {
            return t.Path("just_name");
        }
    }
}
