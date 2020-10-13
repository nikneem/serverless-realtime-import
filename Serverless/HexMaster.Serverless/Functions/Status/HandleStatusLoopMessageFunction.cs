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
    public static class HandleStatusLoopMessageFunction
    {
        [FunctionName("HandleStatusLoopMessageFunction")]
        public static async Task Run(
            [ServiceBusTrigger(QueueNames.StatusLoop, Connection = "ServiceBusConnectionString")] Message msg,
            [ServiceBus(QueueNames.StatusLoop, Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> statusLoop,
            [ServiceBus(QueueNames.StatusMessage, Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> statusMessages,
            [Table(TableNames.Status, PartitionKeys.Status, "{CorrelationId}")] ImportStatusEntity status,
            [Table(TableNames.Status)] CloudTable table,
            [Table(TableNames.StatusProcess)] CloudTable statusProcTable,
            string correlationId,
            ILogger log)
        {
            await statusProcTable.CreateIfNotExistsAsync();
            await table.CreateIfNotExistsAsync();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                log.LogWarning("CorrelationID is empty, generating a new one ({correlationId})", correlationId);
                return;
            }

            if (status == null)
            {
                log.LogWarning("Oops, the status entry of import ({correlationId}) could not be found", correlationId);
                return;

            }

            var statusUpdateCommand = msg.Convert<ImportStatusReportCommand>();
            var updateCounts = await GetProcessingCountsAsync(correlationId, statusProcTable);

            status.TotalSucceeded += updateCounts.Item1;
            status.TotalFailed += updateCounts.Item2;
            status.LastModificationOn = DateTimeOffset.UtcNow;
            if (!status.CompletedOn.HasValue && status.TotalEntries == status.TotalFailed + status.TotalSucceeded)
            {
                status.CompletedOn = DateTimeOffset.UtcNow;
            }

            var operation = TableOperation.Replace(status);
            await table.ExecuteAsync(operation);

            try
            {
                var importStatus = new ImportStatusMessageDto
                {
                    TotalEntries = status.TotalEntries,
                    ErrorMessage = status.ErrorMessage,
                    CompletedOn = status.CompletedOn,
                    CorrelationId = correlationId,
                    Failed = status.TotalFailed,
                    StartedOn = status.CreatedOn,
                    Succeeded = status.TotalSucceeded
                };
                await statusMessages.AddAsync(importStatus.Convert(correlationId));
                if (updateCounts.Item1 > 0 || updateCounts.Item2 > 0)
                {
                    statusUpdateCommand.StopReportingAt = DateTimeOffset.UtcNow.AddMinutes(30);
                }

                if (!status.CompletedOn.HasValue && statusUpdateCommand.StopReportingAt.CompareTo(DateTimeOffset.UtcNow) > 0)
                {
                    await statusLoop.AddAsync(statusUpdateCommand.Convert(correlationId, 1));
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to send message to status update queueu");
            }

            await statusMessages.FlushAsync();
            await statusLoop.FlushAsync();
            
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {msg}");
        }


        private static async Task<Tuple<int, int>> GetProcessingCountsAsync(string correlationId, CloudTable table)
        {
            var partitionKeyFilter = TableQuery.GenerateFilterCondition(
                nameof(StatusProcessingEntity.PartitionKey),
                QueryComparisons.Equal,
                correlationId);

            int successCount = 0;
            int failedCount = 0;

            var ct = new TableContinuationToken();
            var query = new TableQuery<StatusProcessingEntity>().Where(partitionKeyFilter);
            var deleteBatch = new TableBatchOperation();
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(query, ct);
                var result = segment.Results;
                ct = segment.ContinuationToken;
                foreach (var entry in result)
                {
                    successCount += entry.Success;
                    failedCount += entry.Failed;
                    deleteBatch.Add(TableOperation.Delete(entry));
                    if (deleteBatch.Count == 100)
                    {
                        await table.ExecuteBatchAsync(deleteBatch);
                        deleteBatch = new TableBatchOperation();
                    }
                }
            } while (ct != null);
            if (deleteBatch.Count >0)
            {
                await table.ExecuteBatchAsync(deleteBatch);
            }

            return new Tuple<int, int>(successCount, failedCount);
        }
    }
}
