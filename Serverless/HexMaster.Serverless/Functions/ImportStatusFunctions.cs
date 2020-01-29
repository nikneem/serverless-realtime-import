using System;
using System.Threading.Tasks;
using HexMaster.Import;
using HexMaster.Import.Commands;
using HexMaster.Import.DataTransferObjects;
using HexMaster.Import.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace HexMaster.Serverless.Functions
{
    public static class ImportStatusFunctions
    {
        [FunctionName("CreateNewImportStatus")]
        public static async Task CreateNewImportStatus(
            [QueueTrigger(QueueNames.StatusCreate)] CreateImportStatusCommand statusCommand,
            [Table(TableNames.Statusses)] CloudTable statusTable,
            [SignalR(HubName = SignalRHubNames.ImportHub)] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation($"A new import was initiated with Correlation ID {statusCommand.CorrelationId}");

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
            await signalRMessages.AddAsync(new SignalRMessage { Target = "newImport", Arguments = new object[] { importStatusDto } });
        }

        [FunctionName("UpdateImportStatus")]
        public static async Task UpdateImportStatus(
            [QueueTrigger(QueueNames.StatusUpdate)] UpdateImportStatusCommand statusCommand,
            [Table(TableNames.Statusses)] CloudTable statusTable,
            [SignalR(HubName = SignalRHubNames.ImportHub)] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation($"A new import was initiated with Correlation ID {statusCommand.CorrelationId}");

            var findOperation = TableOperation.Retrieve<ImportStatusEntity>(PartitionKeys.Statusses, statusCommand.CorrelationId.ToString());
            var result = await statusTable.ExecuteAsync(findOperation);
            if (result.Result is ImportStatusEntity ent)
            {
                ent.ETag = "*";

                if (statusCommand.Success)
                {
                    ent.TotalSucceeded += 1;
                }
                else
                {
                    ent.TotalFailed += 1;
                }

                if (ent.TotalSucceeded + ent.TotalFailed == ent.TotalEntries)
                {
                    ent.CompletedOn = DateTimeOffset.UtcNow;
                }

                var importStatusDto = new ImportStatusDto
                {
                    CorrelationId = statusCommand.CorrelationId,
                    TotalEntries = ent.TotalEntries,
                    CompletedOn = ent.CompletedOn,
                    ErrorMessage = ent.ErrorMessage,
                    StartedOn = ent.CreatedOn,
                    Failed = ent.TotalFailed,
                    Succeeded = ent.TotalSucceeded
                };
                await signalRMessages.AddAsync(new SignalRMessage { Target = "updateImport", Arguments = new object[] { importStatusDto } });


                var to = TableOperation.Merge(ent);
                await statusTable.ExecuteAsync(to);
            }
        }

    }
}
