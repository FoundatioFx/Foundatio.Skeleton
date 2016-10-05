using System;
using System.Collections.Generic;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Core.Collections;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Api.Models {
    public class ViewUser : IIdentity, IHaveDates {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string EmailAddress { get; set; }
        public bool EmailNotificationsEnabled { get; set; }
        public bool IsEmailAddressVerified { get; set; }
        public bool IsActive { get; set; }
        public string ProfileImagePath { get; set; }
        public bool IsGlobalAdmin { get; set; }

        public ICollection<Membership> Memberships { get; set; }
        public DataDictionary Data { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}
