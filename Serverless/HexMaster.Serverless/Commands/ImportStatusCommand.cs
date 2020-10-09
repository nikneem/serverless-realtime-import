namespace HexMaster.Serverless.Commands
{
    public sealed class ImportStatusCommand
    {
        public int? TotalEntries { get; set; }
        public int? SucceededUpdateCount { get; set; }
        public int? FailedUpdateCount { get; set; }
        public string ErrorMessage { get; set; }

    }
}