using System;
using Owin;

namespace Foundatio.Skeleton.Api {
    public class Startup {
        public void Configuration(IAppBuilder builder) {
            AppBuilder.Build(builder);
        }
    }
}