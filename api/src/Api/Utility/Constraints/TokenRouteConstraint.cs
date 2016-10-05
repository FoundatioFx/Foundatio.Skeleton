using System;
using System.Web.Http.Routing.Constraints;

namespace Foundatio.Skeleton.Api.Utility {
    public class TokenRouteConstraint : RegexRouteConstraint {
        public TokenRouteConstraint() : base(@"^[a-zA-Z\d]{24,40}$") { }
    }
}