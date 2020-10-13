using System;
using System.Threading.Tasks;
using HexMaster.Serverless.Commands;
using HexMaster.Serverless.Entities;
using HexMaster.Serverless.Helpers;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace HexMaster.Serverless.Functions.Status
{
    public static class HandleStatusProgressMessageFunction
    {
        [FunctionName("HandleStatusProgressMessageFunction")]
        public static async Task Run(
            [ServiceBusTrigger(QueueNames.StatusProcess, Connection = "ServiceBusConnectionString")] Message msg,
            [Table(TableNames.StatusProcess)] CloudTable table,
            string correlationId,
            ILogger log)
        {
            await table.CreateIfNotExistsAsync();
            var message = msg.Convert<ImportStatusChangedCommand>();

            var entity = new StatusProcessingEntity
            {
                PartitionKey = correlationId,
                RowKey = Guid.NewGuid().ToString(),
                Failed = message.Failed,
                Success = message.Succeeded
            };

            log.LogInformation("Adding new entry to status process table");
            var operation = TableOperation.Insert(entity);
            await table.ExecuteAsync(operation);

        }
    }
}
