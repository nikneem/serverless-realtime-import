using System;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace HexMaster.Serverless.Helpers
{
    public static class ServiceBusHelper
    {

        public static Message Convert<T>(this T payload, string correlationId, int delay = 0) where T : class
        {
            var messageBody = JsonConvert.SerializeObject(payload);
            var message = new Message(Encoding.UTF8.GetBytes(messageBody))
            {
                CorrelationId = correlationId,
                SessionId = Guid.NewGuid().ToString(),
            };
            if (delay > 0)
            {
                message.ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(delay);
            }
            return message;
        }

        public static T Convert<T>(this Message message) where T : class
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message), "Cannot convert service bus message to object");
            }
            var jsonContent = Encoding.UTF8.GetString(message.Body);
            return JsonConvert.DeserializeObject<T>(jsonContent);
        }

    }
}