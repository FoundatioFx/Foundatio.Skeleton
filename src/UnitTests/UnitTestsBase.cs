using SimpleInjector;
using System;
using System.IO;
using System.Net.Http;
using Foundatio.Logging.Xunit;
using Xunit.Abstractions;

namespace Foundatio.Skeleton.UnitTests {
    public abstract class UnitTestsBase : TestWithLoggingBase, IDisposable {
        private Container _container;
        private bool _initialized;

        public UnitTestsBase(ITestOutputHelper output) : base(output) { }

        public Container Container {
            get {
                if (!_initialized)
                    Initialize();

                return _container;
            }
        }

        public TService GetService<TService>() where TService : class {
            if (!_initialized)
                Initialize();

            return _container.GetInstance<TService>();
        }

        protected virtual void Initialize() {
            _container = GetDefaultContainer();
            _initialized = true;
        }

        protected virtual void RegisterServices(Container container) {
            var bootstrapper = new Bootstrapper();
            bootstrapper.RegisterServices(container);
        }

        public Container GetDefaultContainer() {
            var container = new Container();
            container.Options.AllowOverridingRegistrations = true;
            RegisterServices(container);

            return container;
        }

        public Container GetEmptyContainer() {
            return new Container();
        }

        protected string GetFileContent(string fileName) {
            return File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\{fileName}");
        }

        protected HttpResponseMessage CreateHttpResponseMessageWithContentStringAs(string content) {
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(content) };
        }

        public virtual void Dispose() {
            _container?.Dispose();
        }
    }
}
