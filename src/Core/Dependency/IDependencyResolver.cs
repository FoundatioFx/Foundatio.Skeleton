using System;
using System.Collections.Generic;

namespace Foundatio.Skeleton.Core.Dependency
{
    public interface IDependencyResolver
    {
        object GetService(Type serviceType);
        IEnumerable<object> GetServices(Type serviceType);
    }

    public interface IDependencyContainer : IDependencyResolver
    {
        void Register(Type serviceType, Type implementation);
        void RegisterSingle(Type serviceType, Type implementation);
    }

    public static class DependencyContainerExtensions
    {
        public static void Register<TService,TImplementation>(this IDependencyContainer container)
        {
            container.Register(typeof(TService),typeof(TImplementation));
        }

        public static void RegisterSingle<TService, TImplementation>(this IDependencyContainer container)
        {
            container.RegisterSingle(typeof(TService), typeof(TImplementation));
        }
    }
}
