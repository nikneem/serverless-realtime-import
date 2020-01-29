using System;

namespace HexMaster.Import.Commands
{
    public sealed class UpdateImportStatusCommand
    {
        public Guid CorrelationId { get; set; }
        public bool Success { get; set; }
    }
}
