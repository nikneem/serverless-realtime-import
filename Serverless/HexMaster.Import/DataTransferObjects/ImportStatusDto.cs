using System;

namespace HexMaster.Import.DataTransferObjects
{
    public class ImportStatusDto
    {

        public Guid CorrelationId { get; set; }
        public int TotalEntries { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }

        public DateTimeOffset StartedOn { get; set; }
        public DateTimeOffset? CompletedOn { get; set; }

        public string ErrorMessage { get; set; }

    }
}
