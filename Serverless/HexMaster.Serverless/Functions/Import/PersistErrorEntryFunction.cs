using System;
using System.Threading.Tasks;
using HexMaster.Serverless.Commands;
using HexMaster.Serverless.Entities;
using HexMaster.Serverless.Helpers;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HexMaster.Serverless.Functions.Import
{
    public static class PersistErrorEntryFunction
    {
        [FunctionName("PersistErrorEntryFunction")]
        public static async Task Run(
            [ServiceBusTrigger(QueueNames.Error, Connection = "ServiceBusConnectionString")]
            Message message,
            string correlationId,
            [Table(TableNames.Error)] CloudTable table,
            [ServiceBus(TopicNames.Status, Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> statusTopic,
            ILogger log)
        {
            await table.CreateIfNotExistsAsync();
            var payload = message.Convert<ImportErrorCommand>();
            var errorEntity = new ErrorEntity
            {
                ErrorMessage = payload.ErrorMessage,
                CorrelationId = correlationId,
                ObjectJson = JsonConvert.SerializeObject(payload.ImportModel),
                Timestamp = DateTimeOffset.UtcNow,
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = PartitionKeys.Errors
            };

            var insertOperation = TableOperation.Insert(errorEntity);
            var result = await table.ExecuteAsync(insertOperation);
            if (result.HttpStatusCode.IsSuccessCode())
            {
                log.LogInformation("Stored error message in persistence");
            }
            else
            {
                log.LogCritical(
                    "Failed to store error message while importing sequence ({correlationId})",
                    correlationId);
            }

            var command = new ImportStatusCommand {FailedUpdateCount = 1};
            await statusTopic.AddAsync(command.Convert(correlationId));
            await statusTopic.FlushAsync();
        }
    }
}
