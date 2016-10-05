using System;
using System.Linq;
using Foundatio.Caching;
using Foundatio.Jobs;
using Foundatio.Logging;
using Foundatio.Queues;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Nest;
using Elasticsearch.Net.ConnectionPool;
using Foundatio.Messaging;
using Foundatio.Repositories.Elasticsearch.Queries.Builders;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Domain.Repositories.Configuration {
    public sealed class AppElasticConfiguration : ElasticConfiguration {
        public AppElasticConfiguration(IQueue<WorkItemData> workItemQueue, ICacheClient cacheClient, IMessageBus messageBus, ILoggerFactory loggerFactory) : base(workItemQueue, cacheClient, messageBus, loggerFactory) {
            AddIndex(Logs = new LogstashIndex(this));
            AddIndex(Organizations = new OrganizationIndex(this));
        }

        protected override void ConfigureSettings(ConnectionSettings settings) {
            settings.SetDefaultTypeNameInferrer(p => p.Name.ToLowerUnderscoredWords());
            settings.SetDefaultPropertyNameInferrer(p => p.ToLowerUnderscoredWords());
        }

        public override void ConfigureGlobalQueryBuilders(ElasticQueryBuilder builder) {
            builder.Register<OrganizationIdQueryBuilder>();

            base.ConfigureGlobalQueryBuilders(builder);
        }

        protected override IConnectionPool CreateConnectionPool() {
            var connectionStrings = Settings.Current.ElasticSearchConnectionString.Split(',').Select(url => new Uri(url));
            return new StaticConnectionPool(connectionStrings);
        }

        public OrganizationIndex Organizations { get; }

        public LogstashIndex Logs { get; }
    }
}
