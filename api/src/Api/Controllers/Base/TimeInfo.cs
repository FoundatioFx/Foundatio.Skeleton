using System;
using Exceptionless.DateTimeExtensions;

namespace Foundatio.Skeleton.Api.Controllers {
    public class TimeInfo {
        public string Field { get; set; }
        public DateTimeRange UtcRange { get; set; }
        public TimeSpan Offset { get; set; }
    }
}