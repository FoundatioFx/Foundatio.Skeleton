using System;
using Foundatio.Repositories.Models;

namespace Foundatio.Skeleton.Domain.Models.WorkItems
{
    public enum ExportType
    {
        Json,
        Csv
    }

    public class ExportWorkItem : FilterWorkItem
    {
        public ExportType Type { get; set; }

        public string OrganizationId { get; set; }

        public string UserId { get; set; }

        public string[] Ids { get; set; }

        public string Sort { get; set; }

        public SortOrder SortOrder { get; set; }

        public string[] Columns { get; set; }
    }
}
