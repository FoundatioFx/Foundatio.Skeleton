using System;
using System.Collections.Generic;

namespace Foundatio.Skeleton.Domain.Models.WorkItems {
    public class UserProfileWorkItem {
        public ICollection<string> UserIds { get; set; } = new List<string>();
    }
}
