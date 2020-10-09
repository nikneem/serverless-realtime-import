using System;
using System.Collections.Generic;
using System.Text;

namespace HexMaster.Serverless.Models
{
    public class ImportStatusMessageDto
    {

        public string CorrelationId { get; set; }
        public int TotalEntries { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }

        public DateTimeOffset StartedOn { get; set; }
        public DateTimeOffset? CompletedOn { get; set; }

        public string ErrorMessage { get; set; }

    }
}
