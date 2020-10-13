using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HexMaster.Serverless.Commands;
using HexMaster.Serverless.Helpers;
using HexMaster.Serverless.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HexMaster.Serverless.Functions.Import
{
    public static class StartNewImportProcessFunction
    {
        [FunctionName("StartNewImportProcessFunction")]
        public static async Task Run(
            [BlobTrigger(BlobContainers.Import + "/{name}")]
            CloudBlockBlob blob,
            [ServiceBus(TopicNames.Validation, Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> validationTopic,
            [ServiceBus(TopicNames.Status, Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> statusTopic,
            [ServiceBus(QueueNames.StatusLoop, Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> statusLoopQueue,
            string name,
            ILogger log)
        {
            log.LogInformation($"File {name} was found, trying to import it...");

            try
            {
               var blobTextContent = await blob.DownloadTextAsync();
                var importObjects = JsonConvert.DeserializeObject<List<UserImportModelDto>>(blobTextContent);

                var correlationId = Guid.NewGuid().ToString();

                await SendImportStatusCreateCommand(statusTopic, importObjects.Count, null, correlationId);
                var statusReport = new ImportStatusReportCommand
                    { StopReportingAt = DateTimeOffset.UtcNow.AddMinutes(30) };
                await statusLoopQueue.AddAsync(statusReport.Convert(correlationId, 3));

                foreach (var user in importObjects)
                {
                    var message = user.Convert(correlationId, 5);
                    await validationTopic.AddAsync(message);
                }
            }
            catch (Exception ex)
            {
                await SendImportStatusCreateCommand(
                    statusTopic, 
                    0, 
                    "The file could not be processed. No import was done.", 
                    Guid.NewGuid().ToString());
                log.LogError(ex, "Failed to import file, unknown file format");
            }

            await statusTopic.FlushAsync();
            await validationTopic.FlushAsync();
            await blob.DeleteIfExistsAsync();
            await statusLoopQueue.FlushAsync();
        }

        private static async Task SendImportStatusCreateCommand(
            IAsyncCollector<Message> statusTopic, 
            int importCount, 
            string errorMessage, 
            string correlationId)
        {
            var createImportStatusCommand = new ImportStatusCommand
            {
                TotalEntries = importCount ,
                ErrorMessage = errorMessage
            };
            var statusMessage = createImportStatusCommand.Convert(correlationId);
            await statusTopic.AddAsync(statusMessage);
        }
    }
}
