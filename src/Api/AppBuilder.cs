using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Routing;
using Exceptionless;
using Exceptionless.NLog;
using Foundatio.Jobs;
using Foundatio.Logging;
using Foundatio.Skeleton.Api.MessageBus;
using Foundatio.Skeleton.Api.Security;
using Foundatio.Skeleton.Api.Serialization;
using Foundatio.Skeleton.Api.Utility;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.Jobs;
using Foundatio.Skeleton.Core.Serialization;
using Foundatio.Skeleton.Domain;
using Foundatio.Skeleton.Domain.Repositories;
using Foundatio.Skeleton.Domain.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Newtonsoft.Json;
using Owin;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;
using Swashbuckle.Application;
using LogLevel = Exceptionless.Logging.LogLevel;
using Foundatio.Skeleton.Core.Utility;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;

namespace Foundatio.Skeleton.Api {
    public static class AppBuilder {
        public static void Build(IAppBuilder app, Container container = null) {
            var loggerFactory = Settings.GetLoggerFactory();
            var logger = loggerFactory.CreateLogger("AppBuilder");

            if (container == null)
                container = CreateContainer(loggerFactory, logger);

            var config = new HttpConfiguration();
            if (!String.IsNullOrEmpty(Settings.Current.ExceptionlessApiKey))
                ExceptionlessClient.Default.Configuration.ApiKey = Settings.Current.ExceptionlessApiKey;

            ExceptionlessClient.Default.Configuration.SetVersion(Settings.Current.Version);
            ExceptionlessClient.Default.Configuration.UseLogger(new NLogExceptionlessLog(LogLevel.Warn));
            ExceptionlessClient.Default.RegisterWebApi(config);

            config.Services.Add(typeof(IExceptionLogger), new NLogExceptionLogger());
            config.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(container);
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.JsonFormatter.SerializerSettings = JsonHelper.DefaultSerializerSettings;

            SetupRoutes(config);
            //Config.EnableSystemDiagnosticsTracing();

            config.Filters.Add(new FluentValidationExceptionHandler());
            config.Filters.Add(new UnauthorizedAccessExceptionHandler());
            container.RegisterSingleton<JsonSerializer>(JsonSerializer.Create(new JsonSerializerSettings { ContractResolver = new SignalRContractResolver() }));
            container.RegisterSingleton(app);
            container.RegisterSingleton(config);

            VerifyContainer(container);

            config.MessageHandlers.Add(container.GetInstance<XHttpMethodOverrideDelegatingHandler>());
            config.MessageHandlers.Add(container.GetInstance<EncodingDelegatingHandler>());
            config.MessageHandlers.Add(container.GetInstance<AuthMessageHandler>());

            // Throttle api calls to X every 15 minutes by IP address.
            config.MessageHandlers.Add(container.GetInstance<ThrottlingHandler>());

            EnableCors(config, app);

            // used to allow bootstrappers to participate in configuration
            container.RunStartupActionsAsync().GetAwaiter().GetResult();

            // setup local file storage if not using cloud storage
            if (!Settings.Current.EnableAzureStorage && !Settings.Current.EnableS3Storage && !String.IsNullOrEmpty(Settings.Current.StorageFolder)) {
                var path = PathHelper.ExpandPath($"{Settings.Current.StorageFolder}\\public");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                app.UseFileServer(new FileServerOptions {
                    RequestPath = new PathString("/public"),
                    FileSystem = new PhysicalFileSystem(path),
                });
            }

            app.UseWebApi(config);
            SetupSignalR(app, container);
            SetupSwagger(config);

            var context = new OwinContext(app.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");

            CreateSampleDataAsync(container).GetAwaiter().GetResult();

            RunJobs(container, app, token, loggerFactory, logger);
        }

        private static void RunJobs(Container container, IAppBuilder app, CancellationToken token, ILoggerFactory loggerFactory, ILogger logger) {
            if (!Settings.Current.RunJobsInProcess) {
                logger.Info("Jobs running out of process.");
                return;
            }

            new JobRunner(container.GetInstance<MailMessageJob>(), loggerFactory).RunInBackground(token);
            new JobRunner(container.GetInstance<WorkItemJob>(), loggerFactory, instanceCount: 2).RunInBackground(token);

            logger.Warn("Jobs running in process.");
        }

        private static void EnableCors(HttpConfiguration config, IAppBuilder app) {
            var exposedHeaders = new List<string> { "ETag", "Link", "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-Result-Count" };
            app.UseCors(new CorsOptions {
                PolicyProvider = new CorsPolicyProvider {
                    PolicyResolver = context => {
                        var policy = new CorsPolicy {
                            AllowAnyHeader = true,
                            AllowAnyMethod = true,
                            AllowAnyOrigin = true,
                            SupportsCredentials = true,
                            PreflightMaxAge = 60 * 5
                        };

                        policy.ExposedHeaders.AddRange(exposedHeaders);
                        return Task.FromResult(policy);
                    }
                }
            });

            var enableCorsAttribute = new EnableCorsAttribute("*", "*", "*") {
                SupportsCredentials = true,
                PreflightMaxAge = 60 * 5
            };

            enableCorsAttribute.ExposedHeaders.AddRange(exposedHeaders);
            config.EnableCors(enableCorsAttribute);
        }

        public static void SetupRoutes(HttpConfiguration config) {
            var constraintResolver = new DefaultInlineConstraintResolver();
            constraintResolver.ConstraintMap.Add("objectid", typeof(ObjectIdRouteConstraint));
            constraintResolver.ConstraintMap.Add("objectids", typeof(ObjectIdsRouteConstraint));
            constraintResolver.ConstraintMap.Add("token", typeof(TokenRouteConstraint));
            constraintResolver.ConstraintMap.Add("tokens", typeof(TokensRouteConstraint));
            config.MapHttpAttributeRoutes(constraintResolver);
        }

        private static void SetupSignalR(IAppBuilder app, Container container) {
            if (!Settings.Current.EnableSignalR)
                return;

            var resolver = container.GetInstance<IDependencyResolver>();
            app.MapSignalR<MessageBusConnection>("/api/v1/push", new ConnectionConfiguration { Resolver = resolver });
            container.GetInstance<MessageBusBroker>().Start();
        }

        private static void SetupSwagger(HttpConfiguration config) {
            config.EnableSwagger("schema/{apiVersion}", c => {
                c.SingleApiVersion("v1", "Foundatio Skeleton");
                c.ApiKey("access_token").In("header").Name("access_token").Description("API Key Authentication");
                c.BasicAuth("basic").Description("Basic HTTP Authentication");
                c.IncludeXmlComments($@"{AppDomain.CurrentDomain.BaseDirectory}\bin\Foundatio.Skeleton.Api.xml");
                c.IgnoreObsoleteActions();
            }).EnableSwaggerUi("docs/{*assetPath}", c => {
                c.InjectStylesheet(typeof(AppBuilder).Assembly, "Foundatio.Skeleton.Api.Content.docs.css");
                c.InjectJavaScript(typeof(AppBuilder).Assembly, "Foundatio.Skeleton.Api.Content.docs.js");
            });
        }

        private static async Task CreateSampleDataAsync(Container container) {
            if (Settings.Current.AppMode != AppMode.Local)
                return;

            var userRepository = container.GetInstance<IUserRepository>();
            if (await userRepository.CountAsync().AnyContext() != 0)
                return;

            var sampleDataService = container.GetInstance<SampleDataService>();
            await sampleDataService.CreateTestDataAsync().AnyContext();
        }

        public static Container CreateContainer(ILoggerFactory loggerFactory, ILogger logger, bool includeInsulation = true) {
            var container = new Container();
            container.Options.DefaultScopedLifestyle = new WebApiRequestLifestyle();
            container.Options.AllowOverridingRegistrations = true;
            container.Options.ResolveUnregisteredCollections = true;

            Foundatio.Skeleton.Domain.Bootstrapper.RegisterServices(container, loggerFactory);
            Bootstrapper.RegisterServices(container, loggerFactory);

            if (!includeInsulation)
                return container;

            Assembly insulationAssembly = null;
            try {
                insulationAssembly = Assembly.Load("Foundatio.Skeleton.Insulation");
            } catch (Exception ex) {
                logger.Error(ex, "Unable to load the insulation assembly.");
            }

            if (insulationAssembly != null) {
                var bootstrapperType = insulationAssembly.GetType("Foundatio.Skeleton.Insulation.Bootstrapper");
                if (bootstrapperType == null)
                    return container;

                bootstrapperType.GetMethod("RegisterServices", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { container, loggerFactory });
            }

            return container;
        }

        [Conditional("DEBUG")]
        private static void VerifyContainer(Container container) {
            try {
                container.Verify();
            } catch (Exception ex) {
                var tempEx = ex;
                while (!(tempEx is ReflectionTypeLoadException)) {
                    if (tempEx.InnerException == null)
                        break;
                    tempEx = tempEx.InnerException;
                }

                var typeLoadException = tempEx as ReflectionTypeLoadException;
                if (typeLoadException != null) {
                    foreach (var loaderEx in typeLoadException.LoaderExceptions)
                        Debug.WriteLine(loaderEx.Message);
                }

                Debug.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
