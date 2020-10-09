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

namespace HexMaster.Serverless.Functions.Import
{
    public static class PersistEntryFunction
    {
        [FunctionName("PersistEntryFunction")]
        public static async Task Run(
            [ServiceBusTrigger(QueueNames.Persistence, Connection = "ServiceBusConnectionString")]
            Message message,
            string correlationId,
            [Table(TableNames.User)] CloudTable table,
            [ServiceBus(TopicNames.Status, Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> statusTopic,
            ILogger log)
        {
            await table.CreateIfNotExistsAsync();
            var payload = message.Convert<UserImportModelDto>();

            var entity = new UserEntity
            {
                PartitionKey = PartitionKeys.Users,
                RowKey = payload.Id.ToString(),
                Age = payload.Age,
                Timestamp = DateTimeOffset.UtcNow,
                Address = payload.Address,
                Email = payload.Email,
                EyeColor = payload.EyeColor,
                FavoriteFruit = payload.FavoriteFruit,
                Gender = payload.Gender,
                Greeting = payload.Greeting,
                IsActive = payload.IsActive,
                Name = payload.Name,
                Phone = payload.Phone,
                ETag = "*"
            };

            
            var operation = TableOperation.InsertOrReplace(entity);
            var result = await table.ExecuteAsync(operation);
            if (result.HttpStatusCode.IsSuccessCode())
            {
                log.LogInformation("User ({userId}) successfully stored in persistence store", payload.Id);
            }
            else
            {
                log.LogCritical("Failed to store user ({user}) in persistence store", payload);
            }

            var command = new ImportStatusCommand { SucceededUpdateCount = 1 };
            await statusTopic.AddAsync(command.Convert(correlationId));
            await statusTopic.FlushAsync();

        }
    }
}
