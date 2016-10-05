using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Foundatio.Skeleton.Api.Utility {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class InvalidModelStateFilterAttribute : ActionFilterAttribute {
        public override void OnActionExecuting(HttpActionContext context) {
            if (context == null)
                throw new ArgumentNullException("context");

            if (!context.ModelState.IsValid)
                context.Response = context.Request.CreateErrorResponse(HttpStatusCode.BadRequest, context.ModelState);
        }
    }
}