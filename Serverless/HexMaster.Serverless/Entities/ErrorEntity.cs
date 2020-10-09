using System;
using Microsoft.Azure.Cosmos.Table;

namespace HexMaster.Serverless.Entities
{
    public class ErrorEntity : TableEntity
    {

        public string CorrelationId { get; set; }
        public string ErrorMessage { get; set; }
        public string ObjectJson { get; set; }

    }
}
