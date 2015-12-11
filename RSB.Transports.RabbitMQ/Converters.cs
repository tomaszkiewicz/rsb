using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;

namespace RSB.Transports.RabbitMQ
{
    static class Converters
    {
        public static BasicProperties ToBasicProperties(this MessageProperties properties)
        {
            var basicProperties = new BasicProperties()
            {
                ContentType = properties.ContentType,
                ContentEncoding = properties.ContentEncoding,
                Type = properties.Type,
            };

            if (!string.IsNullOrWhiteSpace(properties.ExceptionType))
            {
                if (basicProperties.Headers == null)
                    basicProperties.Headers = new Dictionary<string, object>();

                basicProperties.Headers["exceptionType"] = properties.ExceptionType;
            }

            if (properties.CorrelationId != null)
                basicProperties.CorrelationId = properties.CorrelationId;

            if (properties.ReplyTo != null)
                basicProperties.ReplyTo = properties.ReplyTo;

            if (properties.ExpirationMilliseconds > 0)
                basicProperties.Expiration = (properties.ExpirationMilliseconds * 1000).ToString();

            return basicProperties;
        }

        public static MessageProperties ToMessageProperties(this IBasicProperties properties)
        {
            var messageProperties = new MessageProperties
            {
                ExpirationMilliseconds = ParseExpirationSeconds(properties.Expiration),
                ContentType = properties.ContentType,
                ContentEncoding = properties.ContentEncoding,
                CorrelationId = properties.CorrelationId,
                ReplyTo = properties.ReplyTo,
                Type = properties.Type
            };
            
            if (properties.Headers != null && properties.Headers.ContainsKey("exceptionType"))
                messageProperties.ExceptionType = Encoding.UTF8.GetString((byte[])properties.Headers["exceptionType"]);

            return messageProperties;
        }

        private static int ParseExpirationSeconds(string s)
        {
            if (s == null)
                return 0;

            int result;

            if (int.TryParse(s, out result))
                return result * 1000;

            return 0;
        }

        public static Message<T> ToMessage<T>(this BasicDeliverEventArgs item, T body)
        {
            return new Message<T>()
            {
                Properties = item.BasicProperties.ToMessageProperties(),
                Body = body
            };
        }
    }
}
