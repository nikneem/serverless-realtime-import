using System;
using System.Threading.Tasks;
using HexMaster.Import;
using HexMaster.Import.Commands;
using HexMaster.Import.DataTransferObjects;
using HexMaster.Import.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace HexMaster.Serverless.Functions
{
    public static class ImportStatusFunctions
    {
        [FunctionName("CreateNewImportStatus")]
        public static async Task CreateNewImportStatus(
            [QueueTrigger(QueueNames.StatusCreate)] CreateImportStatusCommand statusCommand,
            [Queue(QueueNames.StatusProcessing)] IAsyncCollector<ProcessCorralationCommand> statusProcessCommandsQueue,
            [Table(TableNames.Statusses)] CloudTable statusTable,
            [SignalR(HubName = SignalRHubNames.ImportHub)]
            IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation($"A new import was initiated with Correlation ID {statusCommand.CorrelationId}");


            await statusProcessCommandsQueue.AddAsync(new ProcessCorralationCommand
            {
                CorrelationId = statusCommand.CorrelationId
            });

            var entity = new ImportStatusEntity
            {
                PartitionKey = PartitionKeys.Statusses,
                RowKey = statusCommand.CorrelationId.ToString(),
                CreatedOn = DateTimeOffset.UtcNow,
                LastModificationOn = DateTimeOffset.UtcNow,
                TotalEntries = statusCommand.TotalImportEntries,
                ErrorMessage = statusCommand.ErrorMessage,
                CompletedOn = string.IsNullOrEmpty(statusCommand.ErrorMessage)
                    ? null
                    : (DateTimeOffset?) DateTimeOffset.UtcNow
            };
            var operation = TableOperation.InsertOrReplace(entity);
            await statusTable.ExecuteAsync(operation);

            var importStatusDto = new ImportStatusDto
            {
                CorrelationId = statusCommand.CorrelationId,
                TotalEntries = statusCommand.TotalImportEntries,
                CompletedOn = entity.CompletedOn,
                ErrorMessage = statusCommand.ErrorMessage,
                StartedOn = entity.CreatedOn,
                Failed = 0,
                Succeeded = 0
            };
            await signalRMessages.AddAsync(new SignalRMessage
                {Target = "newImport", Arguments = new object[] {importStatusDto}});
        }

        [FunctionName("UpdateImportStatus")]
        public static async Task UpdateImportStatus(
            [QueueTrigger(QueueNames.StatusUpdate)] UpdateImportStatusCommand statusCommand,
            [Table(TableNames.StatusProcessings)] CloudTable statusTable,
            ILogger log)
        {
            log.LogInformation($"A new import was initiated with Correlation ID {statusCommand.CorrelationId}");

            var statusEntity = new StatusProcessingEntity
            {
                PartitionKey = statusCommand.CorrelationId.ToString(),
                RowKey = Guid.NewGuid().ToString(),
                Success = statusCommand.Success ? 1 : 0,
                Failed = statusCommand.Success ? 0 : 1,
                Timestamp = DateTimeOffset.UtcNow,
                ETag = "*"
            };

            var op = TableOperation.Insert(statusEntity);
            await statusTable.ExecuteAsync(op);
        }

        [FunctionName("ProcessStatusMessages")]
        public static async Task ProcessStatusMessages(
            [QueueTrigger(QueueNames.StatusProcessing)] ProcessCorralationCommand correlationCommand,
            [Queue(QueueNames.StatusProcessing)] CloudQueue statusProcessCommandsQueue,
            [Table(TableNames.Statusses)] CloudTable statusTable,
            [Table(TableNames.StatusProcessings)] CloudTable statusProcessingTable,
            [SignalR(HubName = SignalRHubNames.ImportHub)] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation($"A new import was initiated with Correlation ID {correlationCommand.CorrelationId}");

            var op = TableOperation.Retrieve<ImportStatusEntity>(PartitionKeys.Statusses,
                correlationCommand.CorrelationId.ToString());
            var entityResult = await statusTable.ExecuteAsync(op);
            if (entityResult.Result is ImportStatusEntity entity)
            {
                var ownerFilter = TableQuery.GenerateFilterCondition(nameof(StatusProcessingEntity.PartitionKey),
                    QueryComparisons.Equal, correlationCommand.CorrelationId.ToString());

                var query = new TableQuery<StatusProcessingEntity>().Where(ownerFilter);
                var ct = new TableContinuationToken();
                var success = 0;
                var failed = 0;
                var removalBatch = new TableBatchOperation();
                do
                {
                    var statusProcessEntries = await statusProcessingTable.ExecuteQuerySegmentedAsync(query, ct);
                    foreach (var status in statusProcessEntries.Results)
                    {
                        removalBatch.Add(TableOperation.Delete(status));
                        success += status.Success;
                        failed += status.Failed;
                        if (removalBatch.Count == 100)
                        {
                            await statusProcessingTable.ExecuteBatchAsync(removalBatch);
                            removalBatch = new TableBatchOperation();
                        }
                    }

                    ct = statusProcessEntries.ContinuationToken;
                } while (ct != null);

                if (removalBatch.Count > 0)
                {
                    await statusProcessingTable.ExecuteBatchAsync(removalBatch);
                }

                entity.TotalSucceeded += success;
                entity.TotalFailed += failed;
                if (success > 0 || failed > 0)
                {
                    entity.LastModificationOn = DateTimeOffset.UtcNow;
                }
                if (entity.TotalSucceeded + entity.TotalFailed == entity.TotalEntries)
                {
                    entity.CompletedOn = DateTimeOffset.UtcNow;
                }
                var runningTime = DateTimeOffset.UtcNow - entity.LastModificationOn;
                if (!entity.CompletedOn.HasValue  && runningTime.TotalMinutes < 10)
                {
                    var cloudQueueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(correlationCommand));
                    await statusProcessCommandsQueue.AddMessageAsync(
                        cloudQueueMessage, 
                        TimeSpan.MaxValue,
                         TimeSpan.FromMilliseconds(300),
                        null, null);
                }

                var updateOperation = TableOperation.Replace(entity);
                await statusTable.ExecuteAsync(updateOperation);


                var importStatusDto = new ImportStatusDto
                {
                    CorrelationId = correlationCommand.CorrelationId,
                    TotalEntries = entity.TotalEntries,
                    CompletedOn = entity.CompletedOn,
                    ErrorMessage = entity.ErrorMessage,
                    StartedOn = entity.CreatedOn,
                    Failed = entity.TotalFailed,
                    Succeeded = entity.TotalSucceeded
                };
                await signalRMessages.AddAsync(new SignalRMessage
                    {Target = "updateImport", Arguments = new object[] {importStatusDto}});

            }

        }

    }
}