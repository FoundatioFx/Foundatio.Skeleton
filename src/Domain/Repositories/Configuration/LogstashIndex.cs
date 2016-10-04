using System;
using System.Collections.Generic;
using Exceptionless.DateTimeExtensions;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Foundatio.Repositories.Elasticsearch.Queries;
using Foundatio.Utility;

namespace Foundatio.Skeleton.Domain.Repositories.Configuration {
    public sealed class LogstashIndex : Index {
        public LogstashIndex(IElasticConfiguration configuration)
            : base(configuration, Settings.Current.AppScopePrefix + "logstash") {
            AddType(LogEventType = new LogEventType(this, "logevent"));
        }

        public LogEventType LogEventType { get; }
    }

    public class LogEventType : TimeSeriesIndexType<LogEvent> {
        public LogEventType(IIndex index, string name = null, Func<LogEvent, DateTime> getDocumentDateUtc = null): base(index, name, getDocumentDateUtc) {}

        public override string[] GetIndexesByQuery(object query) {
            var withIndexesQuery = query as IElasticIndexesQuery;
            if (withIndexesQuery == null)
                return _defaultIndexes;

            var indexes = new List<string>();
            if (withIndexesQuery.Indexes.Count > 0)
                indexes.AddRange(withIndexesQuery.Indexes);

            if (withIndexesQuery.UtcStartIndex.HasValue || withIndexesQuery.UtcEndIndex.HasValue)
                indexes.AddRange(GetIndexes(withIndexesQuery.UtcStartIndex, withIndexesQuery.UtcEndIndex));

            return indexes.Count > 0 ? indexes.ToArray() : _defaultIndexes;
        }

        public string[] GetIndexes(DateTime? utcStart, DateTime? utcEnd) {
            if (!utcStart.HasValue)
                utcStart = SystemClock.UtcNow;

            if (!utcEnd.HasValue || utcEnd.Value < utcStart)
                utcEnd = SystemClock.UtcNow;

            var period = utcEnd.Value - utcStart.Value;
            if (period.GetTotalYears() > 1)
                return new string[0];

            var utcEndOfDay = utcEnd.Value.EndOfDay();

            var indices = new List<string>();
            for (DateTime current = utcStart.Value.StartOfDay(); current <= utcEndOfDay; current = current.AddDays(1))
                indices.Add(GetIndex(current));

            return indices.ToArray();
        }

        public string GetIndex(DateTime utcDate) {
            return $"{Index.Name}-{utcDate:yyyy.MM.dd}";
        }
    }
}