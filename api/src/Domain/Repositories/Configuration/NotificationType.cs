using System;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Foundatio.Skeleton.Domain.Models;
using Nest;
using Foundatio.Skeleton.Core.Collections;

namespace Foundatio.Skeleton.Domain.Repositories.Configuration {
    public sealed class NotificationType : IndexTypeBase<Notification> {
        public NotificationType(IIndex index) : base(index) {
        }

        public override PutMappingDescriptor<Notification> BuildMapping(PutMappingDescriptor<Notification> map) {
            return base.BuildMapping(map)
                .Dynamic(false)
                .IncludeInAll(false)
                .TimestampField(ts => ts.Enabled().Path(u => u.UpdatedUtc).IgnoreMissing(false))
                .Properties(p => p
                    .String(f => f.Name(u => u.OrganizationId).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.OrganizationId))
                    .String(f => f.Name(u => u.UserId).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.UserId))
                    .String(f => f.Name(u => u.Type).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.Type))
                    .String(f => f.Name(u => u.Message).Index(FieldIndexOption.Analyzed).IndexName(Fields.Message).IncludeInAll())
                    .String(f => f.Name(u => u.Readers).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.Readers).IncludeInAll())
                    .Object<DataDictionary>(f => f.Name(u => u.Data).Dynamic(false))
                    .Date(f => f.Name(u => u.CreatedUtc).IndexName(Fields.CreatedUtc))
                    .Date(f => f.Name(u => u.UpdatedUtc).IndexName(Fields.UpdatedUtc))
                );
        }

        public class Fields {
            public const string OrganizationId = "organization";
            public const string UserId = "user";
            public const string CreatedUtc = "created";
            public const string UpdatedUtc = "updated";
            public const string Type = "type";
            public const string Message = "message";
            public const string Readers = "readers";
        }
    }
}
