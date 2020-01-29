using System.Collections.Generic;
using System.Threading.Tasks;
using HexMaster.Import;
using HexMaster.Import.Commands;
using HexMaster.Import.DataTransferObjects;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace HexMaster.Serverless.Functions
{
    public static class ValidationFunctions
    {

        [FunctionName("ValidationQueueHandler")]
        public static async Task CreateNewImportStatus(
            [QueueTrigger(QueueNames.ImportValidation)] ImportEntityCommand<UserImportModelDto> importEntity,
            [Queue(QueueNames.ImportSuccess)]  IAsyncCollector<ImportEntityCommand<UserImportModelDto>> successQueue,
            [Queue(QueueNames.ImportFailed)]  IAsyncCollector<ErrorEntityCommand<UserImportModelDto>> failedQueue,
            ILogger log)
        {
            log.LogInformation("Validating incoming user entity");
            var validationSuccess = true;
            var errorMessages = new List<string>();
            if (importEntity.Entity.Age > 80)
            {
                var errorMessage =
                    $"User must be 80 or younger, {importEntity.Entity.Name} is {importEntity.Entity.Age}";
                errorMessages.Add(errorMessage);
                log.LogError(errorMessage);
                validationSuccess = false;
            }

            if (validationSuccess)
            {
                await successQueue.AddAsync(importEntity);
            }
            else
            {
                var command = new ErrorEntityCommand<UserImportModelDto>(importEntity.CorrelationId,
                    importEntity.Entity, string.Join(" # ", errorMessages));
                await failedQueue.AddAsync(command);
            }
        }

    }
}
