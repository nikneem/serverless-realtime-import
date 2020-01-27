using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HexMaster.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HexMaster.Serverless.Functions
{
    public static class RealtimeFunctions
    {

        [FunctionName("ImportSignalRNegotiation")]
        public static SignalRConnectionInfo ImportSignalRNegotiation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = SignalRHubNames.ImportHub + "/negotiate")]  HttpRequestMessage req,
            [SignalRConnectionInfo(HubName = SignalRHubNames.ImportHub)] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }


   }
}
