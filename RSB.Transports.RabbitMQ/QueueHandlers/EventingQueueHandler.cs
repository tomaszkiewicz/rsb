using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RSB.Transports.RabbitMQ.QueueHandlers
{
    internal class EventingQueueHandler : IDisposable
    {
        private readonly Connection _connection;
        private readonly ITaskFactoryInvokeReceiveAction _action;
        private readonly string _exchangeName;
        private readonly string _routingKey;
        private readonly QueueInfo _queueInfo;
        private readonly bool _useDurableExchanges;
        private EventingBasicConsumer _consumer;
        private IModel _channel;
        private volatile bool _enabled;

        public EventingQueueHandler(Connection connection, ITaskFactoryInvokeReceiveAction action, string exchangeName, string routingKey, QueueInfo queueInfo, bool useDurableExchanges)
        {
            _connection = connection;
            _action = action;
            _exchangeName = exchangeName;
            _routingKey = routingKey;
            _queueInfo = queueInfo;
            _useDurableExchanges = useDurableExchanges;

            _connection.ConnectionRestored += OnConnectionRestored;
        }

        void OnConnectionRestored(object sender, System.EventArgs e)
        {
            if (_enabled)
                RestoreExchangeAndQueue();
        }

        public void Start()
        {
            RestoreExchangeAndQueue();

            _enabled = true;
        }

        private void RestoreExchangeAndQueue()
        {
            if (_channel == null)
                _channel = _connection.GetChannel();

            _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, _useDurableExchanges, false, null);

            if (_consumer == null)
            {
                _consumer = new EventingBasicConsumer(_channel);

                _consumer.Received += OnConsumerReceived;
                _consumer.Shutdown += OnConsumerShutdown;

                _channel.QueueDeclare(_queueInfo.Name, _queueInfo.Durable, _queueInfo.Exclusive, _queueInfo.AutoDelete, _queueInfo.Arguments);

                _channel.BasicQos(0, 1, false);
                _channel.BasicConsume(_queueInfo.Name, true, _consumer);
                _channel.QueueBind(_queueInfo.Name, _exchangeName, _routingKey);
            }
        }

        void OnConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            ClearChannelAndConsumer();
        }

        void OnConsumerReceived(object sender, BasicDeliverEventArgs args)
        {
            _action.CallDispatcher(args);
        }

        public void Stop()
        {
            ClearChannelAndConsumer();

            _enabled = false;
        }

        private void ClearChannelAndConsumer()
        {
            if (_channel != null)
            {
                try
                {
                    if (_consumer != null)
                        _channel.BasicCancel(_consumer.ConsumerTag);
                }
                catch
                {
                    // do nothing
                }
                finally
                {
                    _channel = null;
                }
            }

            if (_consumer != null)
            {
                _consumer.Shutdown -= OnConsumerShutdown;
                _consumer.Received -= OnConsumerReceived;

                _consumer = null;
            }
        }

        public void Dispose()
        {
            _connection.ConnectionRestored -= OnConnectionRestored;
        }
    }
}