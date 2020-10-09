using System.Threading.Tasks;
using HexMaster.Serverless.Helpers;
using HexMaster.Serverless.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace HexMaster.Serverless.Functions.SignalR
{
    public static class StatusMessageSignalFunction
    {
        [FunctionName("StatusMessageSignalFunction")]
        public static async Task Run(
            [ServiceBusTrigger(QueueNames.StatusMessage, Connection = "ServiceBusConnectionString", IsSessionsEnabled = true)]Message message,
            [SignalR(HubName = SignalRHubNames.ImportHub)] IAsyncCollector<SignalRMessage> signalRMessages)
        {

            var payload = message.Convert<ImportStatusMessageDto>();
            await signalRMessages.AddAsync(new SignalRMessage
                { Target = "updateImport", Arguments = new object[] { payload } });
        }
    }


}
