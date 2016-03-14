using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using RabbitMQ.Client;
using RSB.Interfaces;
using RSB.Serialization;
using RSB.Transports.RabbitMQ.QueueHandlers;
using RSB.Transports.RabbitMQ.Settings;

namespace RSB.Transports.RabbitMQ
{
    public class RabbitMqTransport : ITransport
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly bool _useDurableExchanges;
        private readonly Connection _connection;

        private readonly object _eventingHandlersLock = new object();
        private readonly Dictionary<string, EventingQueueHandler> _eventingHandlers = new Dictionary<string, EventingQueueHandler>();

        private readonly ISerializer _serializer = new JsonSerializer();
        private readonly TypeResolver _typeResolver = new TypeResolver();

        public static RabbitMqTransport FromConfigurationFile(string connectionName = "")
        {
            var settings = RabbitMqTransportSettings.FromConfigurationFile(connectionName);

            return new RabbitMqTransport(settings);
        }

        public RabbitMqTransport(RabbitMqTransportSettings settings)
            : this(settings.Hostname, settings.Username, settings.Password, settings.VirtualHost, settings.Heartbeat, settings.UseDurableExchanges)
        { }

        public RabbitMqTransport(string hostName, string user = "guest", string password = "guest", string virtualHost = "/", ushort heartbeat = 5, bool useDurableExchanges = true)
            : this(new ConnectionFactory()
            {
                HostName = hostName,
                UserName = user,
                Password = password,
                VirtualHost = virtualHost,
                RequestedHeartbeat = heartbeat,
            }, useDurableExchanges)
        {
            _useDurableExchanges = useDurableExchanges;
        }

        public RabbitMqTransport(IConnectionFactory factory, bool useDurableExchanges = true)
        {
            _useDurableExchanges = useDurableExchanges;
            _connection = new Connection(factory, useDurableExchanges);
        }

        public IBlockingQueueHandler GetRawBlockingQueueHandler(QueueInfo queueInfo = null)
        {
            if (queueInfo == null)
                queueInfo = new QueueInfo();

            return new BlockingQueueHandler(_connection, queueInfo, _useDurableExchanges);
        }

        public void Enqueue<T>(string logicalAddress, MessageProperties properties, T body)
        {
            properties.ContentType = _serializer.ContentType;
            properties.ContentEncoding = _serializer.Encoding;

            _connection.Publish(properties.Type, logicalAddress, properties.ToBasicProperties(), _serializer.Serialize(body));
        }

        public Task<bool> Call<T>(string logicalAddress, MessageProperties properties, T body)
        {
            properties.ContentType = _serializer.ContentType;
            properties.ContentEncoding = _serializer.Encoding;

            return _connection.Call(properties.Type, logicalAddress, properties.ToBasicProperties(), _serializer.Serialize(body));
        }

        public void Broadcast<T>(string logicalAddress, MessageProperties properties, T body)
        {
            properties.ContentType = _serializer.ContentType;
            properties.ContentEncoding = _serializer.Encoding;

            _connection.Publish(properties.Type, logicalAddress, properties.ToBasicProperties(), _serializer.Serialize(body));
        }

        public void Prepare<T>() where T : new()
        {
            _serializer.PrepareSerialization<T>();
            _connection.PreparePublish(GetMessageType<T>());
        }

        public void Subscribe<T>(Action<Message<T>> dispatcher, string logicalAddress, string listenAddress, TaskFactory taskFactory = null) where T : new()
        {
            if (string.IsNullOrWhiteSpace(logicalAddress))
                logicalAddress = "#";

            var exchangeName = GetMessageType<T>();
            var key = exchangeName + "/" + logicalAddress;

            lock (_eventingHandlersLock)
            {
                if (_eventingHandlers.ContainsKey(key))
                    return;

                _logger.Trace("Subscribing exchange {0} with routing key {1} and queue name {2}", exchangeName, logicalAddress, listenAddress);

                var action = new TaskFactoryInvokeReceiveAction<T>(dispatcher, _serializer, _typeResolver, taskFactory);

                var handler = new EventingQueueHandler(_connection, action, exchangeName, logicalAddress, new QueueInfo(listenAddress), _useDurableExchanges);

                _eventingHandlers[key] = handler;

                handler.Start();
            }
        }

        public bool IsConnected { get { return _connection.IsConnected; } }

        public void Shutdown()
        {
            lock (_eventingHandlersLock)
                foreach (var handler in _eventingHandlers.Values)
                    handler.Stop();

            _connection.Shutdown();
        }

        private string GetMessageType<T>()
        {
            return typeof(T).Name;
        }
    }
}