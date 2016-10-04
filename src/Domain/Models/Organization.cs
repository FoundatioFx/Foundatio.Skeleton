using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Foundatio.Repositories.Models;

using Foundatio.Skeleton.Core.Collections;
using Foundatio.Skeleton.Core.Models;

namespace Foundatio.Skeleton.Domain.Models {
    public class Organization : IHaveData, IOwnedByOrganizationWithIdentity, IHaveDates, IVersioned {
        /// <summary>
        /// Unique id that identifies the organization.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the organization.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// If true, the organization has been verified.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Organization invites.
        /// </summary>
        public IList<Invite> Invites { get; set; } = new List<Invite>();

        /// <summary>
        /// Optional data entries that contain additional configuration information for this organization.
        /// </summary>
        public DataDictionary Data { get; set; } = new DataDictionary();

        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }

        string IOwnedByOrganization.OrganizationId {
            get {
                return Id;
            }
            set {
                Id = value;
            }
        }

        public long Version { get; set; }
    }

    public class Invite {
        public string Token { get; set; }
        public string EmailAddress { get; set; }
        public string FullName { get; set; }
        public ICollection<string> Roles { get; set; } = new Collection<string>();
        public string AddedByUserId { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
