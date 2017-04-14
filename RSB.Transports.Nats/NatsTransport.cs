using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NATS.Client;
using RSB.Interfaces;
using RSB.Serialization;

namespace RSB.Transports.Nats
{
    public class NatsTransport : ITransport
    {
        private readonly IConnection _connection;
        private readonly ISerializer _serializer = new JsonSerializer();

        public bool IsConnected => _connection != null && _connection.State == ConnState.CONNECTED;

        public NatsTransport()
        {
            var options = ConnectionFactory.GetDefaultOptions();

            options.Url = "nats://localhost:4222";

            _connection = new ConnectionFactory().CreateConnection(options);
        }

        public void Enqueue<T>(string logicalAddress, MessageProperties properties, T body)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Call<T>(string logicalAddress, MessageProperties properties, T body)
        {
            throw new NotImplementedException();
        }

        public void Broadcast<T>(string logicalAddress, MessageProperties properties, T body)
        {
            var subject = GetSubject<T>(logicalAddress);

            var msg = new Message<T>()
            {
                Body = body,
                Properties = properties
            };

            var payload = _serializer.Serialize(msg);

            _connection.Publish($"broadcast.{subject}", payload);
        }

        public void Prepare<T>() where T : new()
        {
            _serializer.PrepareSerialization<T>();
        }

        public void Subscribe<T>(Action<Message<T>> dispatcher, string logicalAddress, string listenAddress, TaskFactory taskFactory = null) where T : new()
        {
            var subject = GetSubject<T>(logicalAddress);

            var subscription = _connection.SubscribeAsync($"broadcast.{subject}", listenAddress, (sender, args) =>
            {
                var msg = _serializer.Deserialize<Message<T>>(args.Message.Data);

                (taskFactory ?? Task.Factory).StartNew(() => dispatcher(msg));
            });

            subscription.Start();
        }

        public void Shutdown()
        {
            _connection.Close();
        }
        
        private string GetSubject<T>(string logicalAddress)
        {
            // TODO balidate logicalAddress

            var subject = typeof(T).Name;

            if (!string.IsNullOrWhiteSpace(logicalAddress))
                subject += "." + logicalAddress;

            return subject;
        }
    }
}
