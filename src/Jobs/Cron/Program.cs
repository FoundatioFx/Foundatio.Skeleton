using System;
using Foundatio.CronJob;
using Foundatio.Extensions;
using Foundatio.Repositories.Elasticsearch.Jobs;
using Foundatio.ServiceProviders;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Domain;

namespace Foundatio.Skeleton.Jobs {
    public class Program {
        public static void Main() {
            AppDomain.CurrentDomain.SetDataDirectory();
            var loggerFactory = Settings.GetLoggerFactory();
            var serviceProvider = ServiceProvider.GetServiceProvider(Settings.JobBootstrappedServiceProvider, loggerFactory);
            var cronService = serviceProvider.GetService<CronService>();

            // data snapshot every hour
            cronService.Add(() => serviceProvider.GetService<SnapshotJob>(), "0 * * * *");

            // cleanup snapshots every 6 hours
            cronService.Add(() => serviceProvider.GetService<CleanupSnapshotJob>(), "0 */6 * * *");

            // cleanup indices every 6 hours
            cronService.Add(() => serviceProvider.GetService<CleanupIndexesJob>(), "0 */6 * * *");

            cronService.RunAsService();
        }
    }
}
