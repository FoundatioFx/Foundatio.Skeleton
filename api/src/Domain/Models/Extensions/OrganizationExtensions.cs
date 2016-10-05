using System;

namespace Foundatio.Skeleton.Domain.Models {
    public static class OrganizationExtensions {
        public static void MarkVerified(this Organization organization) {
            if (organization == null)
                return;

            organization.IsVerified = true;
        }
    }
}
