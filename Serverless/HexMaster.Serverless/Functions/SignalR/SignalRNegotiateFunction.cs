using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace HexMaster.Serverless.Functions.SignalR
{
    public static class SignalRNegotiateFunction
    {
        [FunctionName("SignalRNegotiateFunction")]
        public static SignalRConnectionInfo Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = SignalRHubNames.ImportHub + "/negotiate")] HttpRequestMessage req,
            [SignalRConnectionInfo(HubName = SignalRHubNames.ImportHub)] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

    }
}
