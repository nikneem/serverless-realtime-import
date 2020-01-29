using System;
using System.Collections.Generic;
using System.Text;

namespace HexMaster.Import.Commands
{
    public class CreateImportStatusCommand
    {

        public Guid CorrelationId { get; set; }
        public int TotalImportEntries { get; set; }
        public string ErrorMessage { get; set; }


    }
}
