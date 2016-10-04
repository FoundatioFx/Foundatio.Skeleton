using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

using AutoMapper;
using FluentValidation;
using Foundatio.Caching;
using Foundatio.Jobs;
using Foundatio.Lock;
using Foundatio.Logging;
using Foundatio.Messaging;
using Foundatio.Metrics;
using Foundatio.Queues;
using Foundatio.Repositories.Elasticsearch;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Foundatio.Repositories.Elasticsearch.Jobs;
using Foundatio.Repositories.Migrations;
using Foundatio.Skeleton.Domain.Repositories;
using Foundatio.Skeleton.Domain.Repositories.Configuration;
using Foundatio.Skeleton.Domain.Services;
using Foundatio.Storage;
using Nest;
using RazorSharpEmail;
using SimpleInjector;
using SimpleInjector.Advanced;

using Foundatio.Skeleton.Core.Dependency;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.Mail;
using Foundatio.Skeleton.Core.Queues.Models;
using Foundatio.Skeleton.Core.Utility;

namespace Foundatio.Skeleton.Domain {
    public class Bootstrapper {
        public static void RegisterServices(Container container, ILoggerFactory loggerFactory) {
            var logger = loggerFactory.CreateLogger<Bootstrapper>();
            logger.Info().Message(() => $"Bootstrapping \"{Process.GetCurrentProcess().ProcessName}\" on \"{Environment.MachineName}\".").Property("data", Settings.Current).Write();

            container.RegisterLogger(loggerFactory);

            container.RegisterSingleton<IDependencyResolver>(() => new SimpleInjectorDependencyContainer(container));
            container.RegisterSingleton<IMetricsClient>(() => new InMemoryMetricsClient());
            
            container.RegisterSingleton<AppElasticConfiguration>();
            container.RegisterSingleton<IIndexType<Migration>>(() => container.GetInstance<AppElasticConfiguration>().Organizations.MigrationType);
            container.RegisterSingleton<IElasticClient>(() => container.GetInstance<AppElasticConfiguration>().Client);

            container.RegisterSingleton<ICacheClient, InMemoryCacheClient>();

            container.RegisterSingleton<IEnumerable<IQueueBehavior<MailMessage>>>(() => new[] { new MetricsQueueBehavior<MailMessage>(container.GetInstance<IMetricsClient>()) });
            container.RegisterSingleton<IEnumerable<IQueueBehavior<WorkItemData>>>(() => new[] { new MetricsQueueBehavior<WorkItemData>(container.GetInstance<IMetricsClient>()) });

            var handlers = new WorkItemHandlers();
            handlers.Register<ReindexWorkItem>(container.GetInstance<ReindexWorkItemHandler>);
            container.RegisterSingleton(handlers);

            container.RegisterSingleton<IQueue<WorkItemData>>(() => new InMemoryQueue<WorkItemData>(behaviors: container.GetAllInstances<IQueueBehavior<WorkItemData>>(), workItemTimeout: TimeSpan.FromHours(1)));
            container.RegisterSingleton<IQueue<MailMessage>>(() => new InMemoryQueue<MailMessage>(behaviors: container.GetAllInstances<IQueueBehavior<MailMessage>>()));

            container.RegisterSingleton<IMessageBus, InMemoryMessageBus>();
            container.RegisterSingleton<IMessagePublisher>(container.GetInstance<IMessageBus>);
            container.RegisterSingleton<IMessageSubscriber>(container.GetInstance<IMessageBus>);
            container.RegisterSingleton<HttpMessageHandler, HttpClientHandler>();

            if (!String.IsNullOrEmpty(Settings.Current.StorageFolder)) {
                try {
                    container.RegisterSingleton<IFileStorage>(new FolderFileStorage($"{Settings.Current.StorageFolder}\\private"));
                    container.RegisterSingleton<IPublicFileStorage>(new PublicFileStorage(new FolderFileStorage($"{Settings.Current.StorageFolder}\\public")));
                } catch (Exception ex) {
                    logger.Error(ex, $"Error setting folder storage: {ex.Message}");
                    container.RegisterSingleton<IFileStorage>(new InMemoryFileStorage());
                    container.RegisterSingleton<IPublicFileStorage>(new PublicFileStorage(new InMemoryFileStorage()));
                }
            } else {
                container.RegisterSingleton<IFileStorage>(new InMemoryFileStorage());
                container.RegisterSingleton<IPublicFileStorage>(new PublicFileStorage(new InMemoryFileStorage()));
            }

            container.Register(typeof(IValidator<>), new[] { typeof(Bootstrapper).Assembly }, Lifestyle.Singleton);

            container.RegisterSingleton<IOrganizationRepository, OrganizationRepository>();
            container.RegisterSingleton<IUserRepository, UserRepository>();
            container.RegisterSingleton<INotificationRepository, NotificationRepository>();
            container.RegisterSingleton<ITokenRepository, TokenRepository>();
            container.RegisterSingleton<ILogRepository, LogRepository>();
            container.RegisterSingleton<IMigrationRepository, MigrationRepository>();
            container.RegisterSingleton<IIndexType<Migration>>(() => container.GetInstance<AppElasticConfiguration>().Organizations.MigrationType);

            container.RegisterSingleton<IEmailGenerator>(() => new RazorEmailGenerator(@"Mail\Templates"));
            container.RegisterSingleton<ITemplatedMailService, TemplatedMailService>();

            if (Settings.Current.AppMode == AppMode.Local) {
                container.RegisterSingleton<IMailSender>(() => new InMemoryMailSender());
            } else {
                container.RegisterSingleton<IMailSender, SmtpMailSender>();
            }

            container.RegisterSingleton<ILockProvider, CacheLockProvider>();
            container.RegisterSingleton<SampleDataService>();

            if (Settings.Current.EnableIndexConfiguration)
                container.AddStartupAction(() => {
                    var config = container.GetInstance<AppElasticConfiguration>();
                    config.ConfigureIndexesAsync().GetAwaiter().GetResult();
                });

            container.AppendToCollection(typeof(Profile), typeof(DomainMappings));
            container.RegisterSingleton<IMapper>(() => {
                var profiles = container.GetAllInstances<Profile>();
                var config = new MapperConfiguration(cfg => {
                    cfg.ConstructServicesUsing(container.GetInstance);

                    foreach (var profile in profiles)
                        cfg.AddProfile(profile);
                });

                return config.CreateMapper();
            });
        }
    }

    public class DomainMappings : Profile {
        protected override void Configure() {
        }
    }
}
