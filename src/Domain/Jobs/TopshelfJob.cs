using System;
using System.Threading;
using System.Threading.Tasks;
using Foundatio.Extensions;
using Foundatio.Jobs;
using Foundatio.Logging;
using Topshelf;

namespace Foundatio.Skeleton.Domain.Jobs {
    public static class TopshelfJob {
        public static int Run<T>(IServiceProvider serviceProvider, int instanceCount = 1, TimeSpan? interval = null, ILoggerFactory loggerFactory = null) where T : class, IJob {
            return Run<T>(serviceProvider.GetService<T>, instanceCount, interval, loggerFactory);
        }

        public static int Run<T>(Func<IJob> jobFactory, int instanceCount = 1, TimeSpan? interval = null, ILoggerFactory loggerFactory = null) where T: IJob {
            var cancellationTokenSource = new CancellationTokenSource();
            Task runTask = null;

            var result = HostFactory.Run(config => {
                config.Service<JobRunner>(s => {
                    s.ConstructUsing(() => new JobRunner(jobFactory, loggerFactory, instanceCount: instanceCount, interval: interval));
                    s.WhenStarted((service, control) => {
                        cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, JobRunner.GetShutdownCancellationToken());
                        runTask = service.RunAsync(cancellationTokenSource.Token);
                        return true;
                    });
                    s.WhenStopped((service, control) => {
                        cancellationTokenSource.Cancel();
                        //runTask?.WaitWithoutException(new CancellationTokenSource(5000).Token);
                        return true;
                    });
                });

                config.SetServiceName(typeof(T).Name);
                config.SetDisplayName($"LM CRM {typeof(T).Name}");
                config.StartAutomatically();
                config.RunAsNetworkService();
            });

            return (int)result;
        }
    }
}
