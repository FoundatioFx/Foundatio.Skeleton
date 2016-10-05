using System;
using Newtonsoft.Json.Linq;

namespace Foundatio.Skeleton.Domain.Models.WorkItems {
    public class BulkEditWorkItem : FilterWorkItem {
        public string OrganizationId { get; set; }

        public string UserId { get; set; }

        public string[] Ids { get; set; }

        public JObject Patch { get; set; }
        public string FieldName { get; set; }
        public object FieldValue { get; set; }
    }
}
