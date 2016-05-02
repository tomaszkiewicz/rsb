using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;

namespace RSB.Transports.RabbitMQ.QueueHandlers
{
    internal class PollingQueueHandler : IDisposable, IPollingQueueHandler
    {
        private readonly Connection _connection;
        private readonly QueueInfo _queueInfo;
        private readonly bool _useDurableExchanges;
        private IModel _channel;
        private readonly List<BindingItem> _bindings = new List<BindingItem>();

        public PollingQueueHandler(Connection connection, QueueInfo queueInfo, bool useDurableExchanges)
        {
            _connection = connection;
            _queueInfo = queueInfo;
            _useDurableExchanges = useDurableExchanges;

            _connection.ConnectionRestored += OnConnectionRestored;
            _connection.ConnectionShutdown += OnConnectionShutdown;

            Restore();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _channel = null;
        }

        private void OnConnectionRestored(object sender, System.EventArgs e)
        {
            Restore();
        }

        private void Restore()
        {
            _channel = _connection.GetChannel();

            _channel.QueueDeclare(_queueInfo.Name, _queueInfo.Durable, _queueInfo.Exclusive, _queueInfo.AutoDelete, _queueInfo.Arguments);

            foreach (var binding in _bindings)
                BindToExchange(binding.ExchangeName, binding.RoutingKey);
        }

        public BasicGetResult GetItem()
        {
            return _channel.BasicGet(_queueInfo.Name, true);
        }

        public void Ack(BasicGetResult item)
        {
            _channel.BasicAck(item.DeliveryTag, false);
        }

        public void BindToExchange(string exchangeName, string routingKey = "#")
        {
            _channel.ExchangeDeclare(exchangeName, ExchangeType.Topic, _useDurableExchanges, false, null);
            _channel.QueueBind(_queueInfo.Name, exchangeName, routingKey);

            if (!_bindings.Any(b => b.ExchangeName == exchangeName && b.RoutingKey == routingKey))
                _bindings.Add(new BindingItem() { ExchangeName = exchangeName, RoutingKey = routingKey });
        }

        public void Dispose()
        {
            _connection.ConnectionRestored -= OnConnectionRestored;
        }
    }
}