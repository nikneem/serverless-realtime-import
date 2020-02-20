using System;

namespace HexMaster.Import.Commands
{
    public class CreateImportStatusCommand
    {

        public Guid CorrelationId { get; set; }
        public int TotalImportEntries { get; set; }
        public string ErrorMessage { get; set; }


    }
}
