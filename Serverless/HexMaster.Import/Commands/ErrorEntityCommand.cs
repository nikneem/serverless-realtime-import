using System;
using System.Collections.Generic;
using System.Text;

namespace HexMaster.Import.Commands
{
    public sealed class ErrorEntityCommand <TEntity>
    {

        public Guid CorrelationId { get; set; }
        public TEntity Entity { get; set; }
        public string ErrorMessage { get; }

        public ErrorEntityCommand(Guid correlationId, TEntity entity, string errorMessage)
        {
            CorrelationId = correlationId;
            Entity = entity;
            ErrorMessage = errorMessage;
        }

    }
}
