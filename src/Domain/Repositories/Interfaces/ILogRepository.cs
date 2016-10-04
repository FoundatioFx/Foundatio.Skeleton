using System;
using System.Threading.Tasks;
using Foundatio.Repositories;
using Foundatio.Repositories.Models;
using Newtonsoft.Json.Linq;

namespace Foundatio.Skeleton.Domain.Repositories {
    public interface ILogRepository : ISearchableReadOnlyRepository<LogEvent> {
        Task<FindResults<JObject>> GetEntriesAsync(string organizationId = null, DateTime? utcStartDate = null, DateTime? utcEndDate = null, string userFilter = null, string query = null, PagingOptions paging = null);
    }
}
