using System;
using System.Web.Http.Routing.Constraints;

namespace Foundatio.Skeleton.Api.Utility {
    public class TokensRouteConstraint : RegexRouteConstraint {
        public TokensRouteConstraint() : base(@"^[a-zA-Z\d]{24,40}(,[a-zA-Z\d]{24,40})*$") { }
    }
}