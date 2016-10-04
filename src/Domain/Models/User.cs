using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Core.Collections;
using Foundatio.Skeleton.Core.Models;

namespace Foundatio.Skeleton.Domain.Models {
    public class User : IHaveData, IIdentity, IHaveDates {
        public string Id { get; set; }

        public string FullName { get; set; }

        public string EmailAddress { get; set; }

        public string Password { get; set; }

        public string Salt { get; set; }

        public string PasswordResetToken { get; set; }

        public DateTime PasswordResetTokenCreated { get; set; }

        public bool EmailNotificationsEnabled { get; set; }

        public bool IsActive { get; set; }

        public bool IsEmailAddressVerified { get; set; }

        public string VerifyEmailAddressToken { get; set; }

        public DateTime VerifyEmailAddressTokenCreated { get; set; }

        public string ProfileImagePath { get; set; }

        public DataDictionary Data { get; set; }

        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }

        /// <summary>
        /// The organizations that the user has access to.
        /// </summary>
        public ICollection<Membership> Memberships { get; set; }

        /// <summary>
        /// General user role (type of user)
        /// </summary>
        public ICollection<string> Roles { get; set; }

        public ICollection<OAuthAccount> OAuthAccounts { get; set; }

        public User() {
            EmailNotificationsEnabled = true;
            IsActive = true;
            OAuthAccounts = new Collection<OAuthAccount>();
            Memberships = new Collection<Membership>();
            Roles = new Collection<string>();
            Data = new DataDictionary();
        }
    }

    public class Membership {
        public Membership() {
            Roles = new List<string>();
        }

        public string OrganizationId { get; set; }
        public ICollection<string> Roles { get; set; }
    }

    public class OAuthAccount : IEquatable<OAuthAccount> {
        public string Provider { get; set; }
        public string ProviderUserId { get; set; }
        public string Username { get; set; }
        public SettingsDictionary ExtraData { get; private set; }

        public OAuthAccount() {
            ExtraData = new SettingsDictionary();
        }

        public string EmailAddress() {
            if (!String.IsNullOrEmpty(Username) && Username.Contains("@"))
                return Username;

            foreach (var kvp in ExtraData) {
                if ((String.Equals(kvp.Key, "email") || String.Equals(kvp.Key, "account_email") || String.Equals(kvp.Key, "preferred_email") || String.Equals(kvp.Key, "personal_email")) && !String.IsNullOrEmpty(kvp.Value))
                    return kvp.Value;
            }

            return null;
        }

        public string FullName() {
            foreach (var kvp in ExtraData.Where(kvp => String.Equals(kvp.Key, "name") && !String.IsNullOrEmpty(kvp.Value)))
                return kvp.Value;

            return !String.IsNullOrEmpty(Username) && Username.Contains(" ") ? Username : null;
        }

        public bool Equals(OAuthAccount other) {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return other.Provider.Equals(Provider) && other.ProviderUserId.Equals(ProviderUserId);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof(OAuthAccount))
                return false;
            return Equals((OAuthAccount)obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = 2153;
                if (Provider != null)
                    hash = hash * 9929 + Provider.GetHashCode();
                if (ProviderUserId != null)
                    hash = hash * 9929 + ProviderUserId.GetHashCode();
                return hash;
            }
        }
    }
}
