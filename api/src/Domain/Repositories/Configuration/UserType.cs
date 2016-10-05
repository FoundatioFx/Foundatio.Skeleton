using System;
using System.Linq;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Foundatio.Skeleton.Domain.Models;
using Nest;
using Foundatio.Skeleton.Core.Collections;
using Foundatio.Skeleton.Domain.Extensions;

namespace Foundatio.Skeleton.Domain.Repositories.Configuration {
    public sealed class UserType : IndexTypeBase<User> {
        public UserType(IIndex index) : base(index) {
        }

        public override PutMappingDescriptor<User> BuildMapping(PutMappingDescriptor<User> map) {
            return map
                .Dynamic(false)
                .TimestampField(ts => ts.Enabled().Path(u => u.UpdatedUtc).IgnoreMissing(false))
                .Properties(p => p
                    .String(f => f.Name(u => u.EmailAddress).IndexName(Fields.EmailAddress).Analyzer("keyword_lowercase"))
                    .String(f => f.Name(u => u.PasswordResetToken).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.PasswordResetToken))
                    .String(f => f.Name(u => u.VerifyEmailAddressToken).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.VerifyEmailAddressToken))
                    .Object<DataDictionary>(f => f.Name(u => u.Data).Dynamic(false))
                    .Object<OAuthAccount>(f => f.Name(o => o.OAuthAccounts.First()).RootPath().Properties(mp => mp
                        .String(fu => fu.Name(m => m.ProviderUserId).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.OAuthAccountProviderUserId))))
                    .Object<Membership>(f => f.Name(o => o.Memberships.First()).RootPath().Properties(mp =>
                        mp.String(fu => fu.Name(m => m.OrganizationId).Index(FieldIndexOption.NotAnalyzed).IndexName(Fields.MembershipOrganizationId))))
                );
        }

        public class Fields {
            public const string OAuthAccountProviderUserId = "oauthuser";
            public const string MembershipOrganizationId = "organization";
            public const string EmailAddress = "email";
            public const string PasswordResetToken = "resettoken";
            public const string VerifyEmailAddressToken = "verifytoken";
        }
    }
}
