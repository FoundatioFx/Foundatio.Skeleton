using System;
using System.Collections.Generic;
using Foundatio.Skeleton.Core.Dependency;
using SimpleInjector;

namespace Foundatio.Skeleton.Core.Utility {
    public class SimpleInjectorDependencyContainer : IDependencyContainer {
        private readonly Container _container;

        public SimpleInjectorDependencyContainer(Container container) {
            _container = container;
        }

        public object GetService(Type serviceType) {
            return _container.GetInstance(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType) {
            return _container.GetAllInstances(serviceType);
        }

        public void Register(Type serviceType, Type implementation) {
            _container.Register(serviceType, implementation);
        }

        public void RegisterSingle(Type serviceType, Type implementation) {
            _container.RegisterSingleton(serviceType, implementation);
        }
    }
}
