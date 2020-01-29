using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HexMaster.Import.Models;

namespace HexMaster.Import
{
    public class TableNames
    {
        public const string Users = "users";
        public const string Statusses = "statusses";
        public const string Errors = "errors";
    }
    public class PartitionKeys
    {
        public const string Users = "user";
        public const string Statusses = "status";
        public const string Errors = "error";
    }

    public class QueueNames
    {
        public const string StatusCreate = "statuscreate";
        public const string StatusUpdate = "statusupdate";
        public const string ImportValidation = "validation";
        public const string ImportSuccess = "success";
        public const string ImportFailed = "failed";
    }
    public class BlobContainers
    {
        public const string ImportFolder = "import";
        public const string ErrorFolder = "errors";
    }

    public class SignalRHubNames
    {
        public const string ImportHub = "import";   
    }
}
