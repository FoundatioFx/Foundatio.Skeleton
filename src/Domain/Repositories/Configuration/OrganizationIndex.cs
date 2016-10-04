using System;
using System.Collections.Generic;
using Foundatio.Repositories.Elasticsearch;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Nest;

namespace Foundatio.Skeleton.Domain.Repositories.Configuration {
    public sealed class OrganizationIndex : VersionedIndex {
        public OrganizationIndex(IElasticConfiguration configuration)
            : base(configuration, Settings.Current.AppScopePrefix + "organization", 18) {
            AddType(OrganizationType = new OrganizationType(this));
            AddType(UserType = new UserType(this));
            AddType(TokenType = new TokenType(this));
            AddType(NotificationType = new NotificationType(this));
            AddType(MigrationType = new MigrationType(this));
        }

        public OrganizationType OrganizationType { get; }
        public UserType UserType { get; }
        public TokenType TokenType { get; }
        public NotificationType NotificationType { get; }
        public MigrationType MigrationType { get; }

        public override CreateIndexDescriptor ConfigureDescriptor(CreateIndexDescriptor idx) {
            idx = base.ConfigureDescriptor(idx);
            idx.NumberOfShards(3);

            var keywordLowercase = new CustomAnalyzer {
                Filter = new List<string> { "lowercase" },
                Tokenizer = "keyword"
            };

            idx
                .Analysis(descriptor => descriptor
                        .Analyzers(bases => bases
                                .Add("keyword_lowercase", keywordLowercase)
                        )
                );

            return idx;
        }
    }
}
