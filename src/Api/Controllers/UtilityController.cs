using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Foundatio.Metrics;
using Foundatio.Queues;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.Utility;
using Foundatio.Skeleton.Domain;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Api.Controllers {
    [RoutePrefix(API_PREFIX)]
    public class UtilityController : AppApiController {
        private readonly IMetricsClient _metricsClient;

        public UtilityController(IMetricsClient metricsClient) {
            _metricsClient = metricsClient;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("notfound")]
        [HttpGet, HttpPut, HttpPatch, HttpPost, HttpHead]
        public IHttpActionResult Http404(string link) {
            return Ok(new {
                Message = "Not found"
            });
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("boom")]
        [HttpGet, HttpPut, HttpPatch, HttpPost, HttpHead]
        public IHttpActionResult Boom() {
            throw new ApplicationException("Boom!");
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("metrics")]
        [Authorize(Roles = AuthorizationRoles.GlobalAdmin)]
        [HttpGet]
        public async Task<IHttpActionResult> Metrics(int? hours = null) {
            var metricStats = _metricsClient as IMetricsClientStats;
            if (metricStats == null)
                return Ok();

            if (!hours.HasValue)
                hours = 4;

            var queueNames = new List<string>(new[] { "mailmessage", "workitemdata" });
 
            var queueStats = new Dictionary<string, QueueStatSummary>();
            foreach (string queueName in queueNames) {
                var s = await metricStats.GetQueueStatsAsync(queueName, null, DateTime.UtcNow.AddHours(-hours.Value)).AnyContext();
                queueStats.Add(queueName, s);
            }

            return Ok(new SystemMetrics { Queues = queueStats });
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("settings")]
        [Authorize(Roles = AuthorizationRoles.GlobalAdmin)]
        [HttpGet]
        public async Task<IHttpActionResult> SettingsRequest() {
            return Ok(Settings.Current);
        }

        [HttpGet]
        [Route("assemblies")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult Assemblies() {
            var details = AssemblyDetail.ExtractAll();
            return Ok(details);
        }

        [HttpGet]
        [Route("version")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult Version() {
            if (Settings.Current.HasAppScope) {
                return Ok(new {
                    Version = Settings.Current.InformationalVersion,
                    Settings.Current.AppScope,
                    AppMode = Settings.Current.AppMode.ToString(),
                    Environment.MachineName
                });
            }

            return Ok(new {
                Version = Settings.Current.InformationalVersion,
                AppMode = Settings.Current.AppMode.ToString(),
                Environment.MachineName
            });
        }
    }

    public class SystemMetrics {
        public IDictionary<string, QueueStatSummary> Queues { get; set; }
    }
}
