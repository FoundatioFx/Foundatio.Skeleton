using FakeItEasy;
using Foundatio.Skeleton.Api.Controllers;
using SimpleInjector;
using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Hosting;


namespace Foundatio.Skeleton.UnitTests {
    public static class TestExtensions {
        public static T FakeConfig<T>(this T controller)
                where T : AppApiController {
            var config = new HttpConfiguration();
            controller.Request = new System.Net.Http.HttpRequestMessage();
            controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;

            return controller;
        }

        public static T GetControllerWithFakeDepencencies<T>(this Container container, string organizationId = null)
             where T : AppApiController {
            var c = GetInstanceWithFakeDependencies<T>(container);
            c.FakeConfig();
            // todo: need to set
            //var req = new HttpRequestMessage();
            //req.SetOwinContext()
            return c;
        }

        public static T GetInstanceWithFakeDependencies<T>(this Container container)
             where T : class {
            var ctors = typeof(T).GetConstructors();
            var registeredTypes = container.GetCurrentRegistrations();

            foreach (var ctor in ctors) {
                var prms = ctor.GetParameters();
                foreach (var prm in prms) {
                    // only create the fake type once
                    if (registeredTypes.Any(r => r.ServiceType == prm.ParameterType))
                        continue;

                    var fakeType = typeof(Fake<>).MakeGenericType(prm.ParameterType);
                    var createdFake = Activator.CreateInstance(fakeType);
                    var fakedObject = createdFake.GetType().GetProperty("FakedObject").GetValue(createdFake, null);
                    container.Register(prm.ParameterType, () => fakedObject, Lifestyle.Singleton);
                }
            }

            return container.GetInstance<T>();
        }
    }
}
