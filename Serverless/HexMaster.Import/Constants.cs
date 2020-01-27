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

    }
    public class PartitionKeys
    {

        public const string Users = "user";

    }

    public class BlobContainers
    {
        public const string ImportFolder = "import";
    }

    public class SignalRHubNames
    {
        public const string ImportHub = "import";   
    }
}
