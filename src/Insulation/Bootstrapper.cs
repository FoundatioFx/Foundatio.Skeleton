using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.Runtime;
using Foundatio.Caching;
using Foundatio.Jobs;
using Foundatio.Logging;
using Foundatio.Messaging;
using Foundatio.Metrics;
using Foundatio.Queues;
using Foundatio.Storage;
using Foundatio.Skeleton.Core.Queues.Models;
using Foundatio.Skeleton.Core.Serialization;
using Foundatio.Skeleton.Core.Utility;
using SimpleInjector;
using StackExchange.Redis;
using Foundatio.Skeleton.Domain;
using Foundatio.Skeleton.Insulation.Redis;

namespace Foundatio.Skeleton.Insulation {
    public class Bootstrapper {
        public static void RegisterServices(Container container, ILoggerFactory loggerFactory) {
            var logger = loggerFactory.CreateLogger<Bootstrapper>();

            if (Settings.Current.EnableRedis) {
                if (Settings.Current.EnableMetricsReporting)
                    container.RegisterSingleton<IMetricsClient>(() => new RedisMetricsClient(container.GetInstance<ConnectionMultiplexer>(), prefix: Settings.Current.AppScope));

                var muxer = ConnectionMultiplexer.Connect(Settings.Current.RedisConnectionString);
                muxer.PreserveAsyncOrder = false;
                container.RegisterSingleton(muxer);

                if (Settings.Current.EnableSignalR)
                    container.RegisterSingleton<IConnectionMapping, RedisConnectionMapping>();

                if (Settings.Current.HasAppScope)
                    container.RegisterSingleton<ICacheClient>(() => new ScopedCacheClient(new RedisHybridCacheClient(container.GetInstance<ConnectionMultiplexer>(), JsonHelper.DefaultFoundatioSerializer), Settings.Current.AppScope));
                else
                    container.RegisterSingleton<ICacheClient>(() => new RedisHybridCacheClient(container.GetInstance<ConnectionMultiplexer>(), JsonHelper.DefaultFoundatioSerializer));

                container.RegisterSingleton<IQueue<MailMessage>>(() => new RedisQueue<MailMessage>(muxer, queueName: GetQueueName<MailMessage>(), behaviors: container.GetAllInstances<IQueueBehavior<MailMessage>>()));
                container.RegisterSingleton<IQueue<WorkItemData>>(() => new RedisQueue<WorkItemData>(muxer, queueName: GetQueueName<WorkItemData>(), behaviors: container.GetAllInstances<IQueueBehavior<WorkItemData>>(), workItemTimeout: TimeSpan.FromHours(1)));

                container.RegisterSingleton<IMessageBus>(() => new RedisMessageBus(muxer.GetSubscriber(), Settings.Current.AppScopePrefix + "messages"));
                container.RegisterSingleton<IMessagePublisher>(container.GetInstance<IMessageBus>);
                container.RegisterSingleton<IMessageSubscriber>(container.GetInstance<IMessageBus>);
            } else
                logger.Warn("Redis is NOT enabled on \"{0}\".", Environment.MachineName);


            if (Settings.Current.EnableS3Storage) {
                var privateConnectionInfo = GetAWSConnectionInfo(Settings.Current.PrivateS3StorageConnectionString);
                container.RegisterSingleton<IFileStorage>(new ScopedFileStorage(new S3Storage(privateConnectionInfo.GetCredentials(), RegionEndpoint.USEast1, privateConnectionInfo.Bucket), Settings.Current.PrivateS3StorageFolder));
                var publicConnectionInfo = GetAWSConnectionInfo(Settings.Current.PublicS3StorageConnectionString);
                container.RegisterSingleton<IPublicFileStorage>(new PublicFileStorage(new ScopedFileStorage(new S3Storage(publicConnectionInfo.GetCredentials(), RegionEndpoint.USEast1, publicConnectionInfo.Bucket), Settings.Current.PublicS3StorageFolder)));
            } else
                logger.Warn("Azure Storage is NOT enabled on \"{0}\".", Environment.MachineName);
        }

        private static AWSConnectionInfo GetAWSConnectionInfo(string connectionString) {
            if (String.IsNullOrEmpty(connectionString))
                return new AWSConnectionInfo();

            var settings = ParseConnectionString(connectionString);

            var connectionInfo = new AWSConnectionInfo();
            if (settings.ContainsKey("AccessKey"))
                connectionInfo.AccessKey = settings["AccessKey"];
            if (settings.ContainsKey("SecretKey"))
                connectionInfo.SecretKey = settings["SecretKey"];
            if (settings.ContainsKey("Bucket"))
                connectionInfo.Bucket = settings["Bucket"];

            return connectionInfo;
        }

        private static Dictionary<string, string> ParseConnectionString(string connectionString) {
            return connectionString.Split(';')
                .Where(kvp => kvp.Contains('='))
                .Select(kvp => kvp.Split(new[] { '=' }, 2))
                .ToDictionary(kvp => kvp[0].Trim(), kvp => kvp[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
        }

        private static string GetQueueName<T>() {
            return String.Concat(Settings.Current.AppScopePrefix, typeof(T).Name);
        }
    }

    public class AWSConnectionInfo {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Bucket { get; set; }

        public AWSCredentials GetCredentials() {
            if (String.IsNullOrEmpty(AccessKey)
                || String.IsNullOrEmpty(SecretKey))
                return new InstanceProfileAWSCredentials();

            return new BasicAWSCredentials(AccessKey, SecretKey);
        }
    }
}
