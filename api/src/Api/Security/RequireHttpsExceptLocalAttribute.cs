using System;
using System.Net.Http;
using System.Web.Http.Controllers;

namespace Foundatio.Skeleton.Api.Security {
    public sealed class RequireHttpsExceptLocalAttribute : RequireHttpsAttribute {
        protected override void HandleNonHttpsRequest(HttpActionContext context) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (HostIsLocal(context.Request))
                return;

            base.HandleNonHttpsRequest(context);
        }

        private bool HostIsLocal(HttpRequestMessage request) {
            return request.IsLocal() || request.RequestUri.Host.Contains("localtest.me") || request.RequestUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        }
    }
}
