using System;
using System.Threading.Tasks;
using HexMaster.Serverless.Commands;
using HexMaster.Serverless.Entities;
using HexMaster.Serverless.Helpers;
using HexMaster.Serverless.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace HexMaster.Serverless.Functions.Status
{
    public static class HandleStatusQueueMessageFunction
    {
        [FunctionName("HandleStatusQueueMessageFunction")]
        public static async Task Run(
            [ServiceBusTrigger(QueueNames.Status, Connection = "ServiceBusConnectionString")] Message msg,
            [ServiceBus(QueueNames.StatusMessage, Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> statusMessages,
            [Table(TableNames.Status)] CloudTable table,
            string correlationId,
            ILogger log)
        {
            await table.CreateIfNotExistsAsync();

            if (string.IsNullOrWhiteSpace( correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                log.LogWarning("CorrelationID is empty, generating a new one ({correlationId})", correlationId);
            }

            var statusUpdateCommand = msg.Convert<ImportStatusCommand>();

            var mergedStatus = new ImportStatusEntity
                {
                    PartitionKey = PartitionKeys.Status,
                    RowKey = correlationId,
                    CreatedOn = DateTimeOffset.UtcNow
                };

            if (!string.IsNullOrWhiteSpace(statusUpdateCommand.ErrorMessage))
            {
                mergedStatus.ErrorMessage = statusUpdateCommand.ErrorMessage;
            }
            if (statusUpdateCommand.TotalEntries.HasValue)
            {
                mergedStatus.TotalEntries = statusUpdateCommand.TotalEntries.Value;
            }
            mergedStatus.LastModificationOn = DateTimeOffset.UtcNow;
            var operation = TableOperation.Insert(mergedStatus);
            await table.ExecuteAsync(operation);

            try
            {
                var importStatus = new ImportStatusMessageDto
                {
                    TotalEntries = mergedStatus.TotalEntries,
                    ErrorMessage = mergedStatus.ErrorMessage,
                    CompletedOn = mergedStatus.CompletedOn,
                    CorrelationId = correlationId,
                    Failed = mergedStatus.TotalFailed,
                    StartedOn = mergedStatus.CreatedOn,
                    Succeeded = mergedStatus.TotalSucceeded
                };
                await statusMessages.AddAsync(importStatus.Convert(correlationId));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to send message to status update queueu");
            }


            await statusMessages.FlushAsync();
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {msg}");
        }
    }
}
