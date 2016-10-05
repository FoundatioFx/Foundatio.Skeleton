using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Domain.Models;
using Microsoft.Owin;
using Foundatio.Skeleton.Domain.Services;

namespace Foundatio.Skeleton.Api.Extensions {
    public static class HttpExtensions {
        public static User GetUser(this HttpRequestMessage message) {
            return message?.GetOwinContext().Get<User>("User");
        }

        public static User GetUser(this IOwinRequest request) {
            return request?.Context.Get<User>("User");
        }

        public static void SetUser(this HttpRequestMessage message, User user) {
            message?.GetOwinContext().Set("User", user);
        }

        public static Organization GetOrganization(this HttpRequestMessage message) {
            return message?.GetOwinContext().Get<Organization>("Organization");
        }

        public static Organization GetOrganization(this IOwinRequest request) {
            return request?.Context.Get<Organization>("Organization");
        }

        public static void SetOrganization(this HttpRequestMessage message, Organization organization) {
            message?.GetOwinContext().Set("Organization", organization);
        }

        public static ClaimsPrincipal GetClaimsPrincipal(this HttpRequestMessage message) {
            var context = message.GetOwinContext();
            return context?.Request?.User?.GetClaimsPrincipal();
        }

        public static AuthType GetAuthType(this HttpRequestMessage message) {
            var principal = message.GetClaimsPrincipal();
            return principal?.GetAuthType() ?? AuthType.Anonymous;
        }

        public static bool CanAccessOrganization(this HttpRequestMessage message, string organizationId) {
            if (message.GetUser().Memberships.Contains(m => m.OrganizationId == organizationId))
                return true;

            return message.IsGlobalAdmin();
        }

        public static bool IsGlobalAdmin(this HttpRequestMessage message) {
            var principal = message.GetClaimsPrincipal();
            return principal != null && principal.IsInRole(AuthorizationRoles.GlobalAdmin);
        }

        public static bool IsAdmin(this HttpRequestMessage message) {
            var principal = message.GetClaimsPrincipal();
            return principal != null && principal.IsInRole(AuthorizationRoles.Admin);
        }

        public static string GetSelectedOrganizationId(this HttpRequestMessage message) {
            var principal = message.GetClaimsPrincipal();
            return principal.GetOrganizationId();
        }

        public static string[] GetUserRoles(this HttpRequestMessage message) {
            var principal = message.GetClaimsPrincipal();
            return principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        }

        public static string GetClientIpAddress(this HttpRequestMessage request) {
            var context = request.GetOwinContext();
            return context?.Request.RemoteIpAddress;
        }

        public static string GetQueryString(this HttpRequestMessage request, string key) {
            var queryStrings = request.GetQueryNameValuePairs();
            if (queryStrings == null)
                return null;

            var match = queryStrings.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (String.IsNullOrEmpty(match.Value))
                return null;

            return match.Value;
        }

        public static string GetCookie(this HttpRequestMessage request, string cookieName) {
            CookieHeaderValue cookie = request.Headers.GetCookies(cookieName).FirstOrDefault();
            return cookie?[cookieName].Value;
        }

        public static AuthInfo GetBasicAuth(this HttpRequestMessage request) {
            var authHeader = request.Headers.Authorization;

            if (authHeader == null || authHeader.Scheme.ToLower() != "basic")
                return null;

            string data = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
            if (String.IsNullOrEmpty(data))
                return null;

            string[] authParts = data.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (authParts.Length != 2)
                return null;

            return new AuthInfo {
                Username = authParts[0],
                Password = authParts[1]
            };
        }
    }

    public class AuthInfo {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
