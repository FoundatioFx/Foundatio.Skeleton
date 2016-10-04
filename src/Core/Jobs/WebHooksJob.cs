using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GoodProspect.Core.Extensions;
using GoodProspect.Core.Queues.Models;
using GoodProspect.Domain.Repositories;
using Foundatio.Jobs;
using Foundatio.Metrics;
using Foundatio.Queues;
using Newtonsoft.Json;
using NLog.Fluent;

namespace GoodProspect.Core.Jobs {
    public class WebHooksJob : JobBase {
        private readonly IQueue<WebHookNotification> _queue;
        private readonly IWebHookRepository _webHookRepository;
        private readonly IMetricsClient _statsClient;

        public WebHooksJob(IQueue<WebHookNotification> queue, IMetricsClient statsClient, IWebHookRepository webHookRepository) {
            _queue = queue;
            _webHookRepository = webHookRepository;
            _statsClient = statsClient;
        }

        protected async override Task<JobResult> RunInternalAsync(CancellationToken cancellationToken) {
            QueueEntry<WebHookNotification> queueEntry = null;
            try {
                queueEntry = _queue.Dequeue();
            } catch (Exception ex) {
                if (!(ex is TimeoutException)) {
                    Log.Error().Exception(ex).Message("An error occurred while trying to dequeue the next WebHookNotification: {0}", ex.Message).Write();
                    return JobResult.FromException(ex);
                }
            }
            if (queueEntry == null)
                return JobResult.Success;

            WebHookNotification body = queueEntry.Value;
            Log.Trace().Organization(body.OrganizationId).Message("Process web hook call: id={0} org={1} url={2}", queueEntry.Id, body.OrganizationId, body.Url).Write();

            var client = new HttpClient();
            try {
                var result = await client.PostAsJsonAsync(body.Url, body.Data.ToJson(Formatting.Indented), cancellationToken);

                if (result.StatusCode == HttpStatusCode.Gone) {
                    _webHookRepository.RemoveByUrl(body.Url);
                    Log.Warn().Organization(body.OrganizationId).Message("Deleting web hook: org={0} url={1}", body.OrganizationId, body.Url).Write();
                }

                queueEntry.Complete();

                Log.Info().Organization(body.OrganizationId).Message("Web hook POST complete: status={0} org={1} url={2}", result.StatusCode, body.OrganizationId, body.Url).Write();
            } catch (Exception ex) {
                queueEntry.Abandon();
                return JobResult.FromException(ex);
            }

            return JobResult.Success;
        }
    }
}