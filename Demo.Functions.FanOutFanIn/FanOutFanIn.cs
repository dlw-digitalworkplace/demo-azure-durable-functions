using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Demo.Functions.FanOutFanIn
{
    public static class FanOutFanIn
    {
        [FunctionName(nameof(FanOutFanIn))]
        public static async Task RunFanOutFanIn(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            // Retrieve files
            var fileList = await context.CallActivityAsync<string[]>(nameof(GetFileList), null);

            // Process files
            await Task.WhenAll(
                fileList.Select(
                    fileName => context.CallActivityAsync<string[]>(nameof(CopyFile), fileName)
                )
            );

            // Notify user
            await context.CallActivityAsync(nameof(NotifyUser), null);
        }

        [FunctionName(nameof(GetFileList))]
        public static string[] GetFileList([ActivityTrigger] object input, ILogger log)
        {
            log.LogInformation("Retrieving files to copy...");

            var fileList = new[]
            {
                "file001.txt",
                "file002.txt",
                "file003.txt",
                "file004.txt",
                "file005.txt",
            };

            log.LogDebug("Successfully retrieved files.");

            return fileList;
        }

        [FunctionName(nameof(CopyFile))]
        public static async Task CopyFile([ActivityTrigger] string fileName, ILogger log)
        {
            log.LogInformation("Copying file '{FileName}'...", fileName);

            await Task.Delay(new Random().Next(1000, 5000));

            log.LogDebug("Successfully copied file '{FileName}'.", fileName);
        }

        [FunctionName(nameof(NotifyUser))]
        public static void NotifyUser([ActivityTrigger] object input, ILogger log)
        {
            log.LogInformation("Notifying user...");

            // Send email, feed message, sms, ...

            log.LogDebug("Successfully notified user.");
        }

        [FunctionName(nameof(StartFanOutFanIn))]
        public static async Task<HttpResponseMessage> StartFanOutFanIn(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(nameof(FanOutFanIn), null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}