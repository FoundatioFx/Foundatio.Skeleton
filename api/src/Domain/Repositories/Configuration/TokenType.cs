using System;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Nest;
using Foundatio.Skeleton.Core.Collections;

namespace Foundatio.Skeleton.Domain.Repositories.Configuration {
    public sealed class TokenType : IndexTypeBase<Models.Token> {
        public TokenType(IIndex index) : base(index) {
        }

        public override PutMappingDescriptor<Models.Token> BuildMapping(PutMappingDescriptor<Models.Token> map) {
            return map
                .Dynamic(false)
                .IncludeInAll(false)
                .TimestampField(ts => ts.Enabled().Path(u => u.UpdatedUtc).IgnoreMissing(false))
                .Properties(p => p
                    .String(f => f.Name(u => u.OrganizationId).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.OrganizationId))
                    .String(f => f.Name(u => u.UserId).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.UserId))
                    .Date(f => f.Name(u => u.CreatedUtc).IndexName(Fields.CreatedUtc))
                    .Date(f => f.Name(u => u.UpdatedUtc).IndexName(Fields.UpdatedUtc))
                    .Date(f => f.Name(u => u.Refresh).IndexName(Fields.Refresh))
                    .Object<DataDictionary>(f => f.Name(u => u.Data).Dynamic(false))
                );
        }

        public class Fields {
            public const string OrganizationId = "organization";
            public const string UserId = "user";
            public const string Refresh = "refresh";
            public const string CreatedUtc = "created";
            public const string UpdatedUtc = "updated";
        }
    }
}
