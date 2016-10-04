using System;
using Foundatio.ServiceProviders;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.Jobs;
using Foundatio.Skeleton.Domain;
using Foundatio.Skeleton.Domain.Jobs;

namespace Foundatio.Skeleton.Jobs {
    public class Program {
        public static int Main() {
            AppDomain.CurrentDomain.SetDataDirectory();
            var loggerFactory = Settings.GetLoggerFactory();
            var serviceProvider = ServiceProvider.GetServiceProvider(Settings.JobBootstrappedServiceProvider, loggerFactory);

            return TopshelfJob.Run<MailMessageJob>(serviceProvider, loggerFactory: loggerFactory);
        }
    }
}
