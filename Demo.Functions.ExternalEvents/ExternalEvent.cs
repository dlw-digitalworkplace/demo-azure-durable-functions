using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Demo.Functions.ExternalEvents
{
    public static class ExternalEvent
    {
        [FunctionName(nameof(ExternalEvent))]
        public static async Task<List<string>> RunExternalEvent(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);

            var outputs = new List<string>();

            await context.CallActivityAsync(nameof(StartApproval), null);

            log.LogInformation("Waiting for approval...");

            await context.WaitForExternalEvent("Approve");

            log.LogInformation("APPROVED!");

            return outputs;
        }

        [FunctionName(nameof(StartApproval))]
        public static void StartApproval([ActivityTrigger] object input, ILogger log)
        {
            log.LogInformation("Starting approval...");
        }

        [FunctionName(nameof(StartExternalEvent))]
        public static async Task<HttpResponseMessage> StartExternalEvent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(nameof(ExternalEvent), null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}