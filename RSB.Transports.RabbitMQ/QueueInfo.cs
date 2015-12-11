using System;
using System.Collections.Generic;

namespace RSB.Transports.RabbitMQ
{
    public class QueueInfo
    {
        public QueueInfo(string queueName)
        {
            Name = queueName;
            Durable = false;
            Exclusive = false;
            AutoDelete = true;
            Arguments = new Dictionary<string, object>();
        }

        public QueueInfo() :
            this(Environment.MachineName + "-" + Guid.NewGuid())
        { }

        public string Name { get; set; }
        public bool Durable { get; set; }
        public bool Exclusive { get; set; }
        public bool AutoDelete { get; set; }
        public IDictionary<string, object> Arguments { get; set; }
    }
}