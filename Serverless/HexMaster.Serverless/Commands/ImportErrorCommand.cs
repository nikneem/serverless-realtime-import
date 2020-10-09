using HexMaster.Serverless.Models;

namespace HexMaster.Serverless.Commands
{
    public sealed class ImportErrorCommand
    {
        public string ErrorMessage { get; set; }
        public UserImportModelDto ImportModel { get; set; }
    }
}
