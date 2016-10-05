using System.Linq;
using System.Threading.Tasks;

using Foundatio.Jobs;
using Foundatio.Logging;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Foundatio.Repositories.Migrations;
using Foundatio.Skeleton.Domain.Repositories.Configuration;
using Nest;

using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Domain.Jobs {
    public class MigrationJob : JobBase {
        private readonly MigrationManager _migrationManager;
        private readonly IElasticClient _client;
        private readonly AppElasticConfiguration _configuration;

        public MigrationJob(ILoggerFactory loggerFactory, MigrationManager migrationManager, IElasticClient client,
            AppElasticConfiguration configuration) : base(loggerFactory) {

            _migrationManager = migrationManager;
            _migrationManager.RegisterAssemblyMigrations<MigrationJob>();

            _client = client;
            _configuration = configuration;
        }

        protected override async Task<JobResult> RunInternalAsync(JobContext context) {
            await _configuration.ConfigureIndexesAsync(null, false).AnyContext();
            
            await _migrationManager.RunAsync().AnyContext();

            var tasks = _configuration.Indexes.OfType<VersionedIndex>().Select(ReindexIfNecessary);
            await Task.WhenAll(tasks).AnyContext();

            return JobResult.Success;
        }

        private async Task ReindexIfNecessary(VersionedIndex index) {
            if (index.Version != await index.GetCurrentVersionAsync().AnyContext())
                await index.ReindexAsync().AnyContext();
        }
    }
}
