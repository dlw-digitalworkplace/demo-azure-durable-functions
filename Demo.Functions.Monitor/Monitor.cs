using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Demo.Functions.Monitor
{
    public static class Monitor
    {
        [FunctionName(nameof(Monitor))]
        public static async Task RunMonitor(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);

            var endTime = context.CurrentUtcDateTime.AddHours(1);

            while (context.CurrentUtcDateTime < endTime)
            {
                var isFinished = await context.CallActivityAsync<bool>(nameof(IsItDone), null);

                if (isFinished)
                {
                    log.LogInformation("It is done!");
                    break;
                }

                var nextCheck = context.CurrentUtcDateTime.AddSeconds(5);
                await context.CreateTimer(nextCheck, CancellationToken.None);
            }
        }

        [FunctionName(nameof(IsItDone))]
        public static bool IsItDone([ActivityTrigger] object input, ILogger log)
        {
            log.LogInformation("Checking if it's done...");

            var isDone = false;
            if (new Random().Next(0, 20) >= 15)
            {
                isDone = true;
            }

            log.LogDebug(isDone ? "It is" : "It is not");

            return isDone;
        }

        [FunctionName(nameof(StartMonitor))]
        public static async Task<HttpResponseMessage> StartMonitor(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(nameof(Monitor), null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}