using Foundatio.Skeleton.Domain.Repositories.Query;

namespace Foundatio.Skeleton.Domain.Models.WorkItems {
    public abstract class FilterWorkItem {
        public SystemFilterQuery SystemFilter { get; set; }
        public string UserFilter { get; set; }
        public string Query { get; set; }
    }
}
