using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Foundatio.Skeleton.Api.Extensions {
    public static class HttpActionContextExtensions {
        public static bool SkipAuthorization(this HttpActionContext actionContext) {
            return actionContext.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any()
               || actionContext.ControllerContext.ControllerDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any();
        }
    }
}