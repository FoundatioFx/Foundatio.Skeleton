using System;
using System.Threading.Tasks;
using FluentValidation;
using Foundatio.Repositories.Elasticsearch;
using Foundatio.Repositories.Elasticsearch.Queries.Builders;
using Foundatio.Repositories.Models;
using Foundatio.Repositories.Queries;
using Newtonsoft.Json.Linq;
using Foundatio.Repositories.Elasticsearch.Queries;
using Foundatio.Skeleton.Domain.Repositories.Configuration;

namespace Foundatio.Skeleton.Domain.Repositories {
    public class LogRepository : ElasticReadOnlyRepositoryBase<LogEvent>, ILogRepository {
        public LogRepository(AppElasticConfiguration configuration)
            : base(configuration.Logs.LogEventType) {
        }

        public async Task<FindResults<JObject>> GetEntriesAsync(string organizationId = null, DateTime? utcStartDate = null, DateTime? utcEndDate = null, string userFilter = null, string query = null, PagingOptions paging = null) {
            if (!utcStartDate.HasValue || utcStartDate == DateTime.MinValue)
                utcStartDate = DateTime.UtcNow.AddDays(-3);

            if (!utcEndDate.HasValue || utcEndDate == DateTime.MinValue)
                utcEndDate = DateTime.UtcNow.AddMinutes(15);

            var options = new CrmQuery()
                .WithFilter(userFilter)
                .WithOrganizationId(organizationId)
                .WithSearchQuery(query, false)
                .WithPaging(paging)
                .WithDateRange(utcStartDate, utcEndDate, "@timestamp")
                .WithSort("@timestamp", SortOrder.Descending)
                .WithIndexes(utcStartDate, utcEndDate);

            return await FindAsAsync<JObject>(options);
        }
    }

    public class LogEvent : IIdentity {
        public string Id { get; set; }
    }

    public class LogEventValidator : AbstractValidator<LogEvent> {}
}
