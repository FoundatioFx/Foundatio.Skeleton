using System;
using Foundatio.Skeleton.Core.Mail;
using SimpleInjector;

namespace Foundatio.Skeleton.IntegrationTests {
    public class Bootstrapper {
        public void RegisterServices(Container container) {
            container.Register<IMailSender, InMemoryMailSender>();
        }
    }
}
