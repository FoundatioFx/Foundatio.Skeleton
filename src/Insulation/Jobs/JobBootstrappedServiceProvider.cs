using System;
using Exceptionless;
using Exceptionless.NLog;
using Foundatio.Logging;
using Foundatio.ServiceProviders;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Domain;
using SimpleInjector;
using SimpleInjector.Extensions.ExecutionContextScoping;
using LogLevel = Exceptionless.Logging.LogLevel;

namespace Foundatio.Skeleton.Insulation.Jobs {
    public class JobBootstrappedServiceProvider : BootstrappedServiceProviderBase {
        protected override IServiceProvider BootstrapInternal(ILoggerFactory loggerFactory) {
            ExceptionlessClient.Default.Configuration.SetVersion(Settings.Current.Version);
            ExceptionlessClient.Default.Configuration.UseLogger(new NLogExceptionlessLog(LogLevel.Warn));
            if (!String.IsNullOrEmpty(Settings.Current.ExceptionlessApiKey))
                ExceptionlessClient.Default.Configuration.ApiKey = Settings.Current.ExceptionlessApiKey;
            ExceptionlessClient.Default.Startup();
            ExceptionlessClient.Default.SubmitLog("JobBootstrappedServiceProvider",  "Startup", LogLevel.Warn);

            var container = new Container();
            container.Options.DefaultScopedLifestyle = new ExecutionContextScopeLifestyle();
            container.Options.AllowOverridingRegistrations = true;
            container.Options.ResolveUnregisteredCollections = true;

            container.RegisterSingleton<IServiceProvider>(this);
            Foundatio.Skeleton.Domain.Bootstrapper.RegisterServices(container, loggerFactory);
            Bootstrapper.RegisterServices(container, loggerFactory);

#if DEBUG
            container.Verify();
#endif

            container.RunStartupActionsAsync().GetAwaiter().GetResult();

            return container;
        }
    }
}
