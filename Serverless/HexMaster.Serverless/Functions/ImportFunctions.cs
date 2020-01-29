using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HexMaster.Import;
using HexMaster.Import.Commands;
using HexMaster.Import.DataTransferObjects;
using HexMaster.Import.Entities;
using HexMaster.Import.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace HexMaster.Serverless.Functions
{
    public static class ImportFunctions
    {
        [FunctionName("ImportFunctions")]
        public static async Task Run(
            [BlobTrigger(BlobContainers.ImportFolder + "/{name}")]
            CloudBlockBlob blob,
            [Queue(QueueNames.StatusCreate)] IAsyncCollector<CreateImportStatusCommand> statusCommandsQueue,
            [Queue(QueueNames.StatusProcessing)] IAsyncCollector<ProcessCorralationCommand> statusProcessCommandsQueue,
            [Queue(QueueNames.ImportValidation)]
            IAsyncCollector<ImportEntityCommand<UserImportModelDto>> validationQueue,
            string name,
            ILogger log)
        {
            log.LogInformation($"File {name} was found, trying to import it...");

            try
            {
                var blobTextContent = await blob.DownloadTextAsync();
                var importObjects = JsonConvert.DeserializeObject<List<UserImportModelDto>>(blobTextContent);

                var correlationId = Guid.NewGuid();

                await statusCommandsQueue.AddAsync(new CreateImportStatusCommand
                {
                    CorrelationId = correlationId,
                    TotalImportEntries = importObjects.Count
                });
                foreach (var user in importObjects)
                {
                    var importEntity = new ImportEntityCommand<UserImportModelDto>(correlationId, user);
                    await validationQueue.AddAsync(importEntity);
                }

                await statusProcessCommandsQueue.AddAsync(new ProcessCorralationCommand
                    {CorrelationId = correlationId});
            }
            catch (Exception ex)
            {
                await statusCommandsQueue.AddAsync(new CreateImportStatusCommand
                {
                    CorrelationId = Guid.NewGuid(),
                    TotalImportEntries = 0,
                    ErrorMessage = "The file could not be processed. No import was done."
                });
                log.LogError(ex, "Failed to import file, unknown file format");
            }

            await blob.DeleteIfExistsAsync();
        }


        [FunctionName("ImportEntityCommandHandler")]
        public static async Task ImportEntityCommandHandler(
            [QueueTrigger(QueueNames.ImportSuccess)]
            ImportEntityCommand<UserImportModelDto> importEntity,
            [Queue(QueueNames.StatusUpdate)] IAsyncCollector<UpdateImportStatusCommand> statusUpdateQueue,
            [Queue(QueueNames.ImportFailed)] IAsyncCollector<ErrorEntityCommand<UserImportModelDto>> failedQueue,
            [Table(TableNames.Users)] CloudTable usersTable,
            ILogger log)
        {
            try
            {
                var entity = new UserEntity
                {
                    PartitionKey = PartitionKeys.Users,
                    RowKey = importEntity.Entity.Id.ToString(),
                    Age = importEntity.Entity.Age,
                    Name = importEntity.Entity.Name,
                    Address = importEntity.Entity.Address,
                    Email = importEntity.Entity.Email,
                    EyeColor = importEntity.Entity.EyeColor,
                    FavoriteFruit = importEntity.Entity.FavoriteFruit,
                    Gender = importEntity.Entity.Gender,
                    Greeting = importEntity.Entity.Greeting,
                    IsActive = importEntity.Entity.IsActive,
                    Phone = importEntity.Entity.Phone,
                    Timestamp = DateTimeOffset.UtcNow,
                    ETag = "*"
                };
                var operation = TableOperation.InsertOrReplace(entity);
                await usersTable.ExecuteAsync(operation);
                await statusUpdateQueue.AddAsync(new UpdateImportStatusCommand
                    {
                        CorrelationId = importEntity.CorrelationId,
                        Success = true
                    }
                );
            }
            catch (Exception ex)
            {
                var command =
                    new ErrorEntityCommand<UserImportModelDto>(importEntity.CorrelationId, importEntity.Entity,
                        ex.Message);

                await failedQueue.AddAsync(command);
            }
        }

        [FunctionName("FailedEntityCommandHandler")]
        public static async Task FailedEntityCommandHandler(
            [QueueTrigger(QueueNames.ImportFailed)]
            ErrorEntityCommand<UserImportModelDto> errorEntity,
            [Queue(QueueNames.StatusUpdate)] IAsyncCollector<UpdateImportStatusCommand> statusUpdateQueue,
            [Table(TableNames.Errors)] CloudTable errorsTable,
            ILogger log)
        {
            var entity = new ErrorEntity
            {
                PartitionKey = PartitionKeys.Errors,
                RowKey = Guid.NewGuid().ToString(),
                CorrelationId = errorEntity.CorrelationId,
                ErrorMessage = errorEntity.ErrorMessage,
                ObjectJson = JsonConvert.SerializeObject(errorEntity.Entity),
                Timestamp = DateTimeOffset.UtcNow,
                ETag = "*"
            };
            var operation = TableOperation.InsertOrReplace(entity);
            await errorsTable.ExecuteAsync(operation);

            await statusUpdateQueue.AddAsync(new UpdateImportStatusCommand
            {
                CorrelationId = errorEntity.CorrelationId,
                Success = false
            });
        }

    }

}