using System;
using Foundatio.Repositories.Models;

namespace Foundatio.Skeleton.Domain.Models.Messaging {
    public class AppEntityChanged : EntityChanged {
        public string OrganizationId { get; set; }
        public string ContactId { get; set; }

        public static AppEntityChanged Create(EntityChanged entityChanged) {
            var appEntityChanged = new AppEntityChanged {
                Id = entityChanged.Id,
                Type = entityChanged.Type,
                ChangeType = entityChanged.ChangeType,
                Data = entityChanged.Data
            };

            if (appEntityChanged.Data.ContainsKey(KnownKeys.OrganizationId)) {
                appEntityChanged.OrganizationId = appEntityChanged.Data[KnownKeys.OrganizationId].ToString();
                appEntityChanged.Data.Remove(KnownKeys.OrganizationId);
            }

            if (appEntityChanged.Data.ContainsKey(KnownKeys.ContactId)) {
                appEntityChanged.ContactId = appEntityChanged.Data[KnownKeys.ContactId].ToString();
                appEntityChanged.Data.Remove(KnownKeys.ContactId);
            }

            return appEntityChanged;
        }

        public class KnownKeys {
            public const string OrganizationId = "organization_id";
            public const string ContactId = "contact_id";
        }
    }
}
