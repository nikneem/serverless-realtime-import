using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HexMaster.Import;
using HexMaster.Import.DataTransferObjects;
using HexMaster.Import.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace HexMaster.Serverless.Functions
{
    public static class ImportFunctions
    {
        [FunctionName("ImportFunctions")]
        public static async Task Run(
            [BlobTrigger(BlobContainers.ImportFolder + "/{name}")]CloudBlockBlob blob,
            [Table(TableNames.Users)] CloudTable userTable,
            [SignalR(HubName = SignalRHubNames.ImportHub)] IAsyncCollector<SignalRMessage> signalRMessages,
            string name, 
            ILogger log)
        {
            log.LogInformation($"File {name} was found, trying to import it...");

            List<UserImportModelDto> importObjects = null;
            try
            {
                var blobTextContent = await blob.DownloadTextAsync();
                importObjects = JsonConvert.DeserializeObject<List<UserImportModelDto>>(blobTextContent);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to import file, unknown file format");
            }

            int totalSucceeded = 0;
            try
            {
                log.LogInformation($"Importing {importObjects.Count} objects");
                var batch = new TableBatchOperation();
                foreach (var user in importObjects)
                {
                    var userEntity = new UserEntity
                    {
                        PartitionKey = PartitionKeys.Users,
                        RowKey = user.Id.ToString(),
                        Phone = user.Phone,
                        Address = user.Address,
                        Age = user.Age,
                        Email = user.Email,
                        EyeColor = user.EyeColor,
                        FavoriteFruit = user.FavoriteFruit,
                        Gender = user.Gender,
                        Greeting = user.Greeting,
                        IsActive = user.IsActive,
                        Name = user.Name,
                        Timestamp = DateTimeOffset.UtcNow,
                        ETag = "*"
                    };
                    batch.Add(TableOperation.InsertOrReplace(userEntity));
                    if (batch.Count == 100)
                    {
                        await userTable.ExecuteBatchAsync(batch);
                        batch = new TableBatchOperation();
                        totalSucceeded += 100;
                    }
                }

                if (batch.Count > 0)
                {
                    await userTable.ExecuteBatchAsync(batch);
                    totalSucceeded += batch.Count;
                }

                var notificationMessage = new ImportResultDto
                {
                    TotalSucceeded = totalSucceeded,
                    TotalFailed = 0
                };

                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = "importCompleteSummary",
                        Arguments = new object[] { notificationMessage }
                    });

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to import file...");
            }


            await blob.DeleteIfExistsAsync();
        }
    }
}
