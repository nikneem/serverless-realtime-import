using System;

namespace HexMaster.Import.Commands
{
    public class ImportEntityCommand<TEntity>
    {

        public ImportEntityCommand()
        {

        }

        public ImportEntityCommand(Guid correlationId, TEntity entity)
        {
            CorrelationId = correlationId;
            Entity = entity;
        }

        public Guid CorrelationId { get; set; }
        public TEntity Entity { get; set; }

    }
}