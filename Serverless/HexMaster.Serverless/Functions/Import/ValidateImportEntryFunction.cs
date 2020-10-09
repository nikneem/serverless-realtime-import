using System.Threading.Tasks;
using HexMaster.Serverless.Commands;
using HexMaster.Serverless.Helpers;
using HexMaster.Serverless.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace HexMaster.Serverless.Functions.Import
{
    public static class ValidateImportEntryFunction
    {
        [FunctionName("ValidateImportEntryFunction")]
        public static async Task Run(
            [ServiceBusTrigger(QueueNames.Validation, Connection = "ServiceBusConnectionString")] Message message,
            [ServiceBus(TopicNames.Persistence, Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> persistence,
            [ServiceBus(TopicNames.Error, Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> error,
            string correlationId,
            ILogger log)
        {
            log.LogInformation("Incoming user validation message");
            var payload = message.Convert<UserImportModelDto>();

            if (payload.Age > 80)
            {
                await SendErrorMessageAsync(error, correlationId, payload, "User is too old, must 80 or younger");
                log.LogInformation("Invalid user passed to the error topic");
            }
            else
            {
                await persistence.AddAsync(payload.Convert(correlationId));
            }

            await error.FlushAsync();
            await persistence.FlushAsync();
        }

        private static Task SendErrorMessageAsync(IAsyncCollector<Message> error, 
            string correlationId, 
            UserImportModelDto payload,
            string errorMessageText)
        {
            var errorModel = new ImportErrorCommand
            {
                ErrorMessage = errorMessageText,
                ImportModel = payload
            };
            var errorMessage = errorModel.Convert(correlationId);
            return error.AddAsync(errorMessage);
        }
    }
}
