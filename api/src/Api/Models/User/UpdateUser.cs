using Foundatio.Skeleton.Core.Collections;

namespace Foundatio.Skeleton.Api.Models {
    public class UpdateUser {
        public string FullName { get; set; }
        public bool EmailNotificationsEnabled { get; set; }
        public string ProfileImagePath { get; set; }
        public DataDictionary Data { get; set; }
    }
}
