using System;
using System.Collections.Generic;
using System.Linq;
using Foundatio.Skeleton.Core.Collections;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Domain.Models {
    public static class UserExtensions {
        public static void AddOAuthAccount(this User user, string providerName, string providerUserId, string username, SettingsDictionary data = null) {
            // check to see if we already have an oauth account on the user..  no need to add it again, right?
            //if (user.OAuthAccounts.Any(o => o.Provider == providerName.ToLowerInvariant() && o.ProviderUserId == providerUserId && o.Username == username))
            //{
            //    // bail
            //}

            var account = new OAuthAccount {
                Provider = providerName.ToLowerInvariant(),
                ProviderUserId = providerUserId,
                Username = username
            };

            if (data != null)
                account.ExtraData.Apply(data);

            user.OAuthAccounts.Add(account);
        }

        public static bool RemoveOAuthAccount(this User user, string providerName, string providerUserId) {
            if (user.OAuthAccounts.Count <= 1 && String.IsNullOrEmpty(user.Password))
                return false;

            var account = user.OAuthAccounts.FirstOrDefault(o => o.Provider == providerName.ToLowerInvariant() && o.ProviderUserId == providerUserId);
            if (account == null)
                return true;

            return user.OAuthAccounts.Remove(account);
        }

        public static void CreateVerifyEmailAddressToken(this User user) {
            if (user == null)
                return;

            user.VerifyEmailAddressToken = StringUtils.GetNewToken();
            user.VerifyEmailAddressTokenCreated = DateTime.UtcNow;
        }

        public static bool HasValidEmailAddressTokenExpiration(this User user) {
            if (user == null)
                return false;

            //  todo:  revisit expiration date
            return user.VerifyEmailAddressTokenCreated != DateTime.MinValue && user.VerifyEmailAddressTokenCreated < DateTime.UtcNow.AddDays(30);
        }

        public static void MarkEmailAddressVerified(this User user) {
            if (user == null)
                return;

            user.IsEmailAddressVerified = true;
            user.VerifyEmailAddressToken = null;
            user.VerifyEmailAddressTokenCreated = DateTime.MinValue;
        }

        public static bool IsValidPassword(this User user, string password) {
            if (string.IsNullOrEmpty(user.Salt) || string.IsNullOrEmpty(user.Password)) {
                return false;
            }

            string encodedPassword = password.ToSaltedHash(user.Salt);
            return string.Equals(encodedPassword, user.Password);
        }

        public static void ResetPasswordResetToken(this User user) {
            if (user == null)
                return;

            user.PasswordResetToken = null;
            user.PasswordResetTokenCreated = DateTime.MinValue;
        }

        public static bool HasValidPasswordResetTokenExpiration(this User user) {
            if (user == null)
                return false;

            return user.PasswordResetTokenCreated != DateTime.MinValue && user.PasswordResetTokenCreated < DateTime.UtcNow.AddHours(24);
        }

        public static bool IsGlobalAdmin(this User user) {
            return user.Roles.Contains(AuthorizationRoles.GlobalAdmin);
        }

        public static bool IsAdmin(this User user, string organizationId) {
            var memberships = user.GetMembershipsWithAdminRole();
            return memberships != null && memberships.Any(m => m.OrganizationId.Equals(organizationId));
        }

        public static IEnumerable<Membership> GetMembershipsWithAdminRole(this User user) {
            return user?.Memberships?.Where(m => m.Roles.Any(r => r == AuthorizationRoles.Admin));
        }

        public static bool AddedMembershipRole(this User user, string organizationId, string role) {
            var roles = AuthorizationRoles.GetScope(role);
            return user.AddedMembershipRoles(organizationId, roles);
        }

        public static bool AddedMembershipRoles(this User user, string organizationId, ICollection<string> roles) {
            if (roles.Contains(AuthorizationRoles.GlobalAdmin))
                return false;

            var membership = user.Memberships.FirstOrDefault(m => m.OrganizationId == organizationId);
            if (membership == null) {
                membership = new Membership {
                    OrganizationId = organizationId,
                    Roles = roles,
                };
                user.Memberships.Add(membership);
                return true;
            }

            if (roles.Any(r => !membership.Roles.Contains(r, StringComparer.OrdinalIgnoreCase))) {
                membership.Roles = membership.Roles.Union(roles, StringComparer.OrdinalIgnoreCase).ToList();
                return true;
            }

            return false;
        }

        public static void AddAdminMembership(this User user, string organizationId) {
            user.AddedAdminMembershipRoles(organizationId);
        }

        public static bool AddedAdminMembershipRoles(this User user, string organizationId) {
            var roles = AuthorizationRoles.AdminScope;
            return user.AddedMembershipRoles(organizationId, roles);
        }

        public static void AddGlobalAdminRole(this User user) {
            user.AddedGlobalAdminRole();
        }

        public static bool AddedGlobalAdminRole(this User user) {
            if (user.Roles.Contains(AuthorizationRoles.GlobalAdmin, StringComparer.OrdinalIgnoreCase))
                return false;

            user.Roles.Add(AuthorizationRoles.GlobalAdmin);
            return true;
        }
    }
}
