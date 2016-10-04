using System;
using System.Threading.Tasks;
using System.Web.Http.Results;
using Foundatio.Queues;
using Xunit;

using Foundatio.Skeleton.Api.Controllers;
using Foundatio.Skeleton.Core.Queues.Models;
using Xunit.Abstractions;
using Foundatio.Metrics;

namespace Foundatio.Skeleton.IntegrationTests.API {
    public class UtilityControllerControllerTests : IntegrationTestsBase {
        public UtilityControllerControllerTests(ITestOutputHelper output) : base(output) {}

        [Fact]
        public async Task Can_get_queue_metrics() {
            var utilityController = GetService<UtilityController>();
            var mailQueue = GetService<IQueue<MailMessage>>();
            var metricsClient = GetService<IMetricsClient>() as IBufferedMetricsClient;
            
            var msg = new MailMessage();
            msg.To.Add("hey@hey.com");
            msg.From = "stuff@there.com";
            msg.Subject = "hey";
            await mailQueue.EnqueueAsync(msg);
            await Task.Delay(100);
            var mailEntry = await mailQueue.DequeueAsync();
            await Task.Delay(55);
            await mailEntry.CompleteAsync();

            if (metricsClient != null)
                await metricsClient.FlushAsync();

            var response = await utilityController.Metrics();
            Assert.NotNull(response);

            var result = response as OkNegotiatedContentResult<SystemMetrics>;
            Assert.NotNull(result);

            Assert.True(result.Content.Queues.ContainsKey("mailmessage"));
            var mailStats = result.Content.Queues["mailmessage"];
            Assert.Equal(1, mailStats.Enqueued.Count);
            Assert.Equal(1, mailStats.Dequeued.Count);
            Assert.Equal(1, mailStats.Completed.Count);
        }
    }
}
