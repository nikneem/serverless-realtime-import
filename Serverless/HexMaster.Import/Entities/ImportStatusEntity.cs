using System;
using Microsoft.Azure.Cosmos.Table;

namespace HexMaster.Import.Entities
{
    public class ImportStatusEntity : TableEntity
    {
        public int TotalEntries { get; set; }
        public int TotalSucceeded { get; set; }
        public int TotalFailed { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset LastModificationOn { get; set; }
        public DateTimeOffset? CompletedOn { get; set; }
        public string ErrorMessage { get; set; }
    }
}