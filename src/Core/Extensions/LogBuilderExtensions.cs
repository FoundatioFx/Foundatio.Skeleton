using System;
using System.Collections.Generic;
using System.Linq;

namespace Foundatio.Logging {
    public static class LogBuilderExtensions {
        public static ILogBuilder Critical(this ILogBuilder builder, bool isCritical = true) {
            return isCritical ? builder.Tag("Critical") : builder;
        }

        public static ILogBuilder Tag(this ILogBuilder builder, string tag) {
            return builder.Tag(new[] { tag });
        }

        public static ILogBuilder Tag(this ILogBuilder builder, IEnumerable<string> tags) {
            var tagList = new List<string>();
            if (builder.LogData.Properties.ContainsKey("tags") && builder.LogData.Properties["tags"] is List<string>)
                tagList = builder.LogData.Properties["tags"] as List<string>;

            foreach (string tag in tags) {
                if (!tagList.Any(s => s.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                    tagList.Add(tag);
            }

            return builder.Property("tags", tagList);
        }

        public static ILogBuilder Organization(this ILogBuilder builder, string organizationId) {
            if (String.IsNullOrEmpty(organizationId))
                return builder;

            return builder.Property("organization", organizationId);
        }

        public static ILogBuilder DataSource(this ILogBuilder builder, string dataSourceId) {
            if (String.IsNullOrEmpty(dataSourceId))
                return builder;

            return builder.Property("datasource", dataSourceId);
        }

        public static ILogBuilder DataSourceInstance(this ILogBuilder builder, string dataSourceInstanceId) {
            if (String.IsNullOrEmpty(dataSourceInstanceId))
                return builder;

            return builder.Property("datasourceinstance", dataSourceInstanceId);
        }

        public static ILogBuilder Properties(this ILogBuilder builder, ICollection<KeyValuePair<string, string>> collection)
        {
            if (collection == null)
                return builder;

            foreach (var pair in collection)
                if (pair.Key != null)
                    builder.Property(pair.Key, pair.Value);

            return builder;
        }

    }
}
