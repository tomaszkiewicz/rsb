using System;
using System.Threading.Tasks;
using RSB.Interfaces;
using RSB.Serialization;
using StackExchange.Redis;

namespace RSB.Transports.Redis
{
    public class RedisTransport : ITransport
    {
        private readonly bool _routingWithoutMessageType;
        private readonly ConnectionMultiplexer _connection;
        private readonly IDatabase _redis;
        private readonly ISerializer _serializer = new JsonSerializer();
        private readonly ISubscriber _subscriber;
        private readonly TypeResolver _typeResolver = new TypeResolver();

        public RedisTransport(string hostname, bool routingWithoutMessageType = false)
        {
            _routingWithoutMessageType = routingWithoutMessageType;
            _connection = ConnectionMultiplexer.Connect(hostname);
            _subscriber = _connection.GetSubscriber();
            _redis = _connection.GetDatabase();
        }

        public void Enqueue<T>(string logicalAddress, MessageProperties properties, T body)
        {
            properties.ContentType = _serializer.ContentType;
            properties.ContentEncoding = _serializer.Encoding;

            var key = GetTargetLogicalAddress(properties.Type, logicalAddress);
            var value = _serializer.Serialize(new Message<T>()
            {
                Body = body,
                Properties = properties
            });

            _redis.ListRightPush(key, value);
        }

        public async Task<bool> Call<T>(string logicalAddress, MessageProperties properties, T body)
        {
            properties.ContentType = _serializer.ContentType;
            properties.ContentEncoding = _serializer.Encoding;

            var key = GetTargetLogicalAddress(properties.Type, logicalAddress);
            var value = _serializer.Serialize(new Message<T>
            {
                Body = body,
                Properties = properties
            });

            await _redis.ListRightPushAsync(key, value);

            return await Task.FromResult(true);
        }

        public void Broadcast<T>(string logicalAddress, MessageProperties properties, T body)
        {
            throw new NotImplementedException();
        }

        public void Prepare<T>() where T : new()
        {
            _serializer.PrepareSerialization<T>();
            _serializer.PrepareSerialization<Message<T>>();
        }

        public void Subscribe<T>(Action<Message<T>> dispatcher, string logicalAddress, string listenAddress, TaskFactory taskFactory = null) where T : new()
        {
            var taskFactoryInvokeReceiveAction = new TaskFactoryInvokeReceiveAction<T>(dispatcher, _serializer, _typeResolver, taskFactory);

            _subscriber.Subscribe(new RedisChannel(listenAddress, RedisChannel.PatternMode.Auto), (channel, value) => Handler(taskFactoryInvokeReceiveAction, channel, value));
        }

        private void Handler(ITaskFactoryInvokeReceiveAction taskFactoryInvokeReceiveAction, RedisChannel channel, RedisValue value)
        {
            var str = value.ToString();

            taskFactoryInvokeReceiveAction.CallDispatcher(str);
        }

        public bool IsConnected { get { return _connection.IsConnected; } }

        public void Shutdown()
        {
            _connection.Dispose();
        }

        private string GetTargetLogicalAddress(string messageType, string logicalAddress)
        {
            return _routingWithoutMessageType ? logicalAddress : messageType + "." + logicalAddress;
        }
    }
}