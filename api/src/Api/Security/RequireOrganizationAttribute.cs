using System;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Foundatio.Skeleton.Api.Extensions;

namespace Foundatio.Skeleton.Api.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireOrganizationAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (actionContext.SkipAuthorization()) return;

            base.OnAuthorization(actionContext);

            if (actionContext.Request.GetSelectedOrganizationId() == null)
                throw new UnauthorizedAccessException("This user is not authorized or is not associated with a valid organization");
        }
    }
}