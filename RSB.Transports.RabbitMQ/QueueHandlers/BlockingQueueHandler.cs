using System.Collections.Generic;
using System.Linq;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RSB.Transports.RabbitMQ.QueueHandlers
{
    internal class BlockingQueueHandler : IBlockingQueueHandler
    {
        private readonly Connection _connection;
        private readonly QueueInfo _queueInfo;
        private readonly bool _useDurableExchanges;
        private QueueingBasicConsumer _consumer;
        private IModel _channel;
        private bool _started;
        private readonly List<BindingItem> _bindings = new List<BindingItem>();

        public BlockingQueueHandler(Connection connection, QueueInfo queueInfo, bool useDurableExchanges)
        {
            _connection = connection;
            _queueInfo = queueInfo;
            _useDurableExchanges = useDurableExchanges;

            _connection.ConnectionRestored += OnConnectionRestored;
        }

        void OnConnectionRestored(object sender, System.EventArgs e)
        {
            if (_started)
                Start();
        }

        public void Start()
        {
            if (_channel == null)
                _channel = _connection.GetChannel();

            if (_consumer == null)
            {
                _consumer = new QueueingBasicConsumer(_channel);

                _channel.QueueDeclare(_queueInfo.Name, _queueInfo.Durable, _queueInfo.Exclusive, _queueInfo.AutoDelete, _queueInfo.Arguments);

                _channel.BasicQos(0, 1, false);
                _channel.BasicConsume(_queueInfo.Name, false, _consumer);
            }

            foreach (var binding in _bindings)
                BindToExchange(binding.ExchangeName, binding.RoutingKey);

            _started = true;
        }

        public void BindToExchange(string exchangeName, string routingKey = "#")
        {
            _channel.ExchangeDeclare(exchangeName, ExchangeType.Topic, _useDurableExchanges, false, null);
            _channel.QueueBind(_queueInfo.Name, exchangeName, routingKey);

            if (!_bindings.Any(b => b.ExchangeName == exchangeName && b.RoutingKey == routingKey))
                _bindings.Add(new BindingItem() { ExchangeName = exchangeName, RoutingKey = routingKey });
        }

        public BasicDeliverEventArgs GetItem(int timeoutMilliseconds = 5000)
        {
            BasicDeliverEventArgs item;

            if (!_consumer.Queue.Dequeue(timeoutMilliseconds, out item))
                return null;

            _channel.BasicAck(item.DeliveryTag, false);

            return item;
        }

        public void Stop()
        {
            if (_consumer == null) return;

            _channel.BasicCancel(_consumer.ConsumerTag);

            _started = false;
        }

        public void Dispose()
        {
            _connection.ConnectionRestored -= OnConnectionRestored;
        }

        class BindingItem
        {
            public string ExchangeName { get; set; }
            public string RoutingKey { get; set; }
        }
    }
}