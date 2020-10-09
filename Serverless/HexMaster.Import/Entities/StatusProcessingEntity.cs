﻿using Microsoft.Azure.Cosmos.Table;

namespace HexMaster.Import.Entities
{
    public sealed class StatusProcessingEntity : TableEntity
    {
        public int Success { get; set; }
        public int Failed { get; set; }
    }
}
