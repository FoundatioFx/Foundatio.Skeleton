using System;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

using AutoMapper;
using Foundatio.Caching;
using Foundatio.Logging;
using Foundatio.Skeleton.Api.MessageBus;
using Foundatio.Skeleton.Api.Models;
using Foundatio.Skeleton.Api.Security;
using Foundatio.Skeleton.Api.Utility;
using SimpleInjector;
using SimpleInjector.Advanced;
using Foundatio.Skeleton.Core.Utility;
using Foundatio.Skeleton.Domain;
using Foundatio.Skeleton.Domain.Models;
using Token = Foundatio.Skeleton.Domain.Models.Token;

namespace Foundatio.Skeleton.Api {
    public class Bootstrapper {
        public static void RegisterServices(Container container, ILoggerFactory loggerFactory) {
            container.Register<MessageBusConnection>();
            container.RegisterSingleton<IConnectionMapping, ConnectionMapping>();
            container.RegisterSingleton<MessageBusBroker>();

            var resolver = new SimpleInjectorSignalRDependencyResolver(container);
            container.RegisterSingleton<IDependencyResolver>(resolver);
            container.RegisterSingleton<IConnectionManager>(() => new ConnectionManager(resolver));

            container.RegisterSingleton<IUserIdProvider, MessageBus.PrincipalUserIdProvider>();
            container.RegisterSingleton<ThrottlingHandler>(() => new ThrottlingHandler(container.GetInstance<ICacheClient>(), userIdentifier => Settings.Current.ApiThrottleLimit, TimeSpan.FromMinutes(15)));
            container.RegisterSingleton<XHttpMethodOverrideDelegatingHandler>();
            container.RegisterSingleton<EncodingDelegatingHandler>();
            container.RegisterSingleton<AuthMessageHandler>();
 
            container.AppendToCollection(typeof(Profile), typeof(ApiMappings));
        }

        public class ApiMappings : Profile {
            protected override void Configure() {
                CreateMap<Notification, ViewNotification>();
                CreateMap<NewNotification, Notification>();

                CreateMap<Organization, ViewOrganization>();
                CreateMap<Organization, NewOrganization>();
                CreateMap<NewOrganization, Organization>();
                CreateMap<ViewOrganization, NewOrganization>();

                CreateMap<Token, ViewToken>();
                CreateMap<NewToken, Token>().ForMember(m => m.Type, m => m.Ignore());

                CreateMap<User, ViewUser>().AfterMap((u, vu) => vu.IsGlobalAdmin = u.IsGlobalAdmin());
                CreateMap<User, UpdateUser>();
                CreateMap<UpdateUser, User>();
            }
        }
    }
}
