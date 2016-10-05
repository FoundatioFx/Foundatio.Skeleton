using System;
using Foundatio.ServiceProviders;
using LearningMachine.Core.Extensions;
using LearningMachine.Domain;
using LearningMachine.Domain.Jobs;

namespace LearningMachine.Jobs {
    public class Program {
        public static int Main() {
            AppDomain.CurrentDomain.SetDataDirectory();
            var loggerFactory = Settings.GetLoggerFactory();
            var serviceProvider = ServiceProvider.GetServiceProvider(Settings.JobBootstrappedServiceProvider, loggerFactory);

            return TopshelfJob.Run<WebHookJob>(serviceProvider, loggerFactory: loggerFactory);
        }
    }
}
