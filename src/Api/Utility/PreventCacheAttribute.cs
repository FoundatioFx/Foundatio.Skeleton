using System;
using System.Net.Http.Headers;
using System.Web.Http.Filters;

namespace Foundatio.Skeleton.Api.Utility
{
    public class PreventCacheAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Response != null)
            {
                actionExecutedContext.Response.Headers.CacheControl = new CacheControlHeaderValue
                {
                    MaxAge = new TimeSpan(0)
                    , MustRevalidate = true
                    , NoCache = true
                    , NoStore = true
                };
            }
            base.OnActionExecuted(actionExecutedContext);
        }
    }
}