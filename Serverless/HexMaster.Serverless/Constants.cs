namespace HexMaster.Serverless
{
    public class Constants
    {
    }

    public static class TableNames
    {
        public const string Status = "status";
        public const string Error = "error";
        public const string User = "user";
    }
    public static class PartitionKeys
    {
        public const string Status = "status";
        public const string Errors = "error";
        public const string Users = "user";
    }

    public static class QueueNames
    {
        public const string Error = "errorqueue";
        public const string Persistence = "persistencequeue";
        public const string Validation = "validatequeue";
        public const string Status = "statusqueue";
        public const string StatusMessage = "statusmessagequeue";
    }


    public static class TopicNames
    {
        public const string Error = "importentryerrortopic";
        public const string Persistence = "importentrypersisttopic";
        public const string Validation = "importentryvalidationtopic";
        public const string Status = "importstatusupdatedtopic";
    }

    public static class BlobContainers
    {
        public const string Import = "import";
    }

    public static class SignalRHubNames
    {
        public const string ImportHub = "import";
    }
}
