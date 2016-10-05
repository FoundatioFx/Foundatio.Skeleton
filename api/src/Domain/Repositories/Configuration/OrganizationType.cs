using System;
using System.Linq;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Foundatio.Skeleton.Domain.Models;
using Nest;
using Foundatio.Skeleton.Core.Collections;
using Foundatio.Skeleton.Domain.Extensions;

namespace Foundatio.Skeleton.Domain.Repositories.Configuration {
    public sealed class OrganizationType : IndexTypeBase<Organization> {
        public OrganizationType(IIndex index) : base(index) {
        }

        public override PutMappingDescriptor<Organization> BuildMapping(PutMappingDescriptor<Organization> map) {
            return base.BuildMapping(map)
                .Dynamic(false)
                .TimestampField(ts => ts.Enabled().Path(u => u.UpdatedUtc).IgnoreMissing(false))
                .Properties(p => p
                    .String(f => f.Name(u => u.Name).Index(FieldIndexOption.Analyzed).IndexName("name").IncludeInAll())
                    .Object<DataDictionary>(f => f.Name(u => u.Data).Dynamic(false))
                    .Object<Invite>(f => f.Name(o => o.Invites.First()).RootPath().Properties(ip => ip
                        .String(fu => fu.Name(i => i.Token).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.InviteToken))))
                );
        }

        public class Fields {
            public const string InviteToken = "invite";
        }
    }
}
