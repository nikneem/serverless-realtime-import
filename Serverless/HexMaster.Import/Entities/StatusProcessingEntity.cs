using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;

namespace HexMaster.Import.Entities
{
    public sealed class StatusProcessingEntity : TableEntity
    {
        public Guid CorrelationId { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
    }
}
