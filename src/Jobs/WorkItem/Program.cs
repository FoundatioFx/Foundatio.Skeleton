using System;
using Foundatio.Jobs;
using Foundatio.ServiceProviders;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Domain;
using Foundatio.Skeleton.Domain.Jobs;

namespace Foundatio.Skeleton.Jobs {
    public class Program {
        public static int Main() {
            AppDomain.CurrentDomain.SetDataDirectory();
            var loggerFactory = Settings.GetLoggerFactory();
            var serviceProvider = ServiceProvider.GetServiceProvider(Settings.JobBootstrappedServiceProvider, loggerFactory);

            return TopshelfJob.Run<WorkItemJob>(serviceProvider, instanceCount: 2, loggerFactory: loggerFactory);
        }
    }
}
