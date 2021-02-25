using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Demo.Functions
{
    public static class HelloDurableWorld
    {
        [FunctionName(nameof(HelloDurableWorld))]
        public static async Task<List<string>> RunHelloDurableWorld(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "delaware"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Digital Workplace"));

            // returns ["delaware", "Digital Workplace"]
            return outputs;
        }

        [FunctionName(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation("Greeting '{Name}'...", name);

            log.LogDebug("'{Name}' was greeted successfully.", name);

            return $"Hello {name}!";
        }

        [FunctionName(nameof(StartHelloDurableWorld))]
        public static async Task<HttpResponseMessage> StartHelloDurableWorld(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(nameof(HelloDurableWorld), null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}