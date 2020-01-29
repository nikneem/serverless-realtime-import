using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;

namespace HexMaster.Import.Entities
{
    public class ErrorEntity : TableEntity
    {

        public Guid CorrelationId { get; set; }
        public string ErrorMessage { get; set; }
        public string ObjectJson { get; set; }

    }
}
