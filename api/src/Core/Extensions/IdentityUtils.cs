using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using GoodProspect.Core.Authorization;
using GoodProspect.Domain.Repositories;
using IIdentity = System.Security.Principal.IIdentity;

namespace GoodProspect.Core.Extensions {
    public static class IdentityUtils {
        public const string TokenAuthenticationType = "Token";
        public const string UserAuthenticationType = "User";
        public const string UserIdClaim = "UserId";
        public const string OrganizationIdClaim = "OrganizationId";
        
        public static ClaimsIdentity ToIdentity(this GoodProspect.Domain.Models.Token token, IUserRepository userRepository)
        {
            if (token == null || token.Type != GoodProspect.Domain.Models.TokenType.Access)
                return WindowsIdentity.GetAnonymous();

            if (!String.IsNullOrEmpty(token.UserId))
                return CreateUserIdentity(token.UserId, token.OrganizationId, userRepository);

            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, token.Id),
                new Claim(OrganizationIdClaim, token.OrganizationId)
            };

            if (token.Scopes.Count > 0)
                claims.AddRange(token.Scopes.Select(scope => new Claim(ClaimTypes.Role, scope)));
            else
                claims.Add(new Claim(ClaimTypes.Role, Authorization.AuthorizationRoles.Client));

            return new ClaimsIdentity(claims, TokenAuthenticationType);
        }

        public static ClaimsIdentity ToIdentity(this GoodProspect.Domain.Models.User user)
        {
            if (user == null)
                return WindowsIdentity.GetAnonymous();

            //TODO: lookup last login org instead of selecting first
            var membership = user.Memberships.FirstOrDefault();

            return CreateUserIdentity(user.EmailAddress, user.Id, user.Roles, membership);
        }

        public static ClaimsIdentity CreateUserIdentity(string userId, string organizationId, IUserRepository userRepository)
        {
            if (String.IsNullOrEmpty(userId))
                throw new ArgumentNullException("userId");
            if (userRepository == null)
                throw new ArgumentNullException("userRepository");

            var user = userRepository.GetById(userId, true);
            if (user == null)
                return WindowsIdentity.GetAnonymous();

            var membership = user.Memberships.FirstOrDefault(m => m.OrganizationId == organizationId);

            return CreateUserIdentity(user.EmailAddress, user.Id, user.Roles, membership);
        }        

        public static ClaimsIdentity CreateUserIdentity(string emailAddress, string userId, ICollection<string> roles, GoodProspect.Domain.Models.Membership membership)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, emailAddress),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(OrganizationIdClaim, membership.OrganizationId)
            };

            var userRoles = new HashSet<string>(roles);
            userRoles.AddRange(membership.Roles);
            if (userRoles.Any())
            {
                // add implied scopes
                if (userRoles.Contains(AuthorizationRoles.GlobalAdmin))
                    userRoles.Add(AuthorizationRoles.User);

                if (userRoles.Contains(AuthorizationRoles.User))
                    userRoles.Add(AuthorizationRoles.Client);

                claims.AddRange(userRoles.Select(scope => new Claim(ClaimTypes.Role, scope)));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, AuthorizationRoles.Client));
                claims.Add(new Claim(ClaimTypes.Role, AuthorizationRoles.User));
            }

            return new ClaimsIdentity(claims, UserAuthenticationType);
        }

        public static AuthType GetAuthType(this IPrincipal principal) {
            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
                return AuthType.Anonymous;

            return IsTokenAuthType(principal) ? AuthType.Token : AuthType.User;
        }

        public static bool IsTokenAuthType(this IPrincipal principal) {
            var identity = GetClaimsIdentity(principal);
            if (identity == null)
                return false;

            return identity.AuthenticationType == TokenAuthenticationType;
        }

        public static bool IsUserAuthType(this IPrincipal principal) {
            var identity = GetClaimsIdentity(principal);
            if (identity == null)
                return false;

            return identity.AuthenticationType == UserAuthenticationType;
        }

        public static ClaimsPrincipal GetClaimsPrincipal(this IPrincipal principal) {
            return principal as ClaimsPrincipal;
        }

        public static ClaimsIdentity GetClaimsIdentity(this IPrincipal principal) {
            var claimsPrincipal = GetClaimsPrincipal(principal);
            if (claimsPrincipal == null)
                return null;

            var identity = claimsPrincipal.Identity as ClaimsIdentity;
            if (identity == null)
                return null;

            return identity;
        }

        public static string GetUserId(this IPrincipal principal) {
            return IsTokenAuthType(principal) ? GetClaimValue(principal, UserIdClaim) : GetClaimValue(principal, ClaimTypes.NameIdentifier);
        }

        public static string GetOrganizationId(this IPrincipal principal) {
            return GetClaimValue(principal, OrganizationIdClaim);
        }

        public static string GetClaimValue(this IPrincipal principal, string type) {
            if (principal == null)
                return null;

            var identity = principal.GetClaimsIdentity();
            if (identity == null)
                return null;

            return GetClaimValue(identity, type);
        }

        public static string GetClaimValue(this IIdentity identity, string type) {
            var claimsIdentity = identity as ClaimsIdentity;
            if (claimsIdentity == null)
                return null;

            var claim = claimsIdentity.FindAll(type).FirstOrDefault();
            if (claim == null)
                return null;

            return claim.Value;
        }
    }
    
    public enum AuthType {
        User,
        Token,
        Anonymous
    }
}
