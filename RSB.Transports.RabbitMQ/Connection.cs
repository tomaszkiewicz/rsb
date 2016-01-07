using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing;
using RSB.Exceptions;

namespace RSB.Transports.RabbitMQ
{
    internal class Connection
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IConnectionFactory _factory;
        private readonly bool _useDurableExchanges;
        private readonly object _connectionLock = new object();
        private IConnection _connection;
        private IModel _callChannel;
        private IModel _publishChannel;
        private volatile bool _shutdown;

        public event EventHandler<ShutdownEventArgs> ConnectionShutdown;
        public event EventHandler ConnectionRestored;

        private readonly ConcurrentDictionary<string, bool> _exchanges = new ConcurrentDictionary<string, bool>();

        private readonly object _publishChannelLock = new object();
        private readonly object _callChannelLock = new object();
        private readonly ChannelTcsIndex _callChannelTcsIndex = new ChannelTcsIndex();
        private readonly object _reconnectLock = new object();
        private Thread _reconnectThread;

        public bool IsConnected
        {
            get { lock (_connectionLock) return _connection != null && _connection.IsOpen; }
        }

        public Connection(IConnectionFactory factory, bool useDurableExchanges)
        {
            _factory = factory;
            _useDurableExchanges = useDurableExchanges;

            StartReconnectThread();
        }

        private void Reconnect()
        {
            lock (_connectionLock)
            {
                lock (_callChannelLock)
                {
                    lock (_publishChannelLock)
                    {
                        if (_callChannel != null)
                        {
                            _callChannel.BasicReturn -= OnBasicReturn;
                            _callChannel.BasicAcks -= OnBasicAcks;

                            try
                            {
                                _callChannel.Dispose();
                            }
                            catch
                            {
                                // do nothing
                            }
                            finally
                            {
                                _callChannel = null;
                            }
                        }

                        if (_publishChannel != null)
                        {
                            try
                            {
                                _publishChannel.Dispose();

                            }
                            catch
                            {
                                // do nothing
                            }
                            finally
                            {
                                _publishChannel = null;
                            }
                        }

                        if (_connection != null)
                        {
                            _connection.ConnectionShutdown -= ConnectionShutdown;
                            _connection.ConnectionShutdown -= OnConnectionShutdown;

                            try
                            {
                                _connection.Dispose();
                            }
                            catch
                            {
                                // do nothing
                            }
                            finally
                            {
                                _connection = null;
                            }
                        }

                        _exchanges.Clear();

                        _connection = _factory.CreateConnection();

                        _connection.ConnectionShutdown += OnConnectionShutdown;
                        _connection.ConnectionShutdown += ConnectionShutdown;

                        _callChannel = _connection.CreateModel();

                        _callChannel.ConfirmSelect();

                        _callChannel.BasicReturn += OnBasicReturn;
                        _callChannel.BasicAcks += OnBasicAcks;

                        _publishChannel = _connection.CreateModel();
                    }
                }
            }
        }

        private void OnBasicAcks(object model, BasicAckEventArgs args)
        {
            var tcs = _callChannelTcsIndex.Remove(args.DeliveryTag);

            if (tcs != null && !tcs.Task.IsCompleted)
                tcs.SetResult(true);
        }

        private void OnBasicReturn(object model, BasicReturnEventArgs args)
        {
            var tcs = _callChannelTcsIndex.Remove(args.BasicProperties.CorrelationId);

            if (tcs != null)
                tcs.SetException(new MessageReturnedException(args.ReplyCode, args.ReplyText));
        }

        public IModel GetChannel()
        {
            lock (_connectionLock)
                return _connection.CreateModel();
        }

        private void ReconnectThreadRun()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        lock (_connectionLock)
                            if (!(_connection == null || !_connection.IsOpen))
                                break;

                        _logger.Info("Trying to reconnect to RabbitMQ...");

                        Reconnect();

                        _logger.Info("Reconnect succeeded. The connection restored. All subscriptions restored.");

                        RaiseConnectionRestored();
                    }
                    catch (BrokerUnreachableException ex)
                    {
                        _logger.Error(ex, "Reconnect failed: RabbitMQ broker is unrechable.");

                        Thread.Sleep(5000);
                    }
                    catch (EndOfStreamException ex)
                    {
                        _logger.Error(ex, "Reconnect failed: EndOfStreamException.");

                        Thread.Sleep(5000);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Reconnect failed due to exception");

                        Thread.Sleep(5000);
                    }
                }
            }
            finally
            {
                lock (_reconnectLock)
                    _reconnectThread = null;
            }
        }

        private void RaiseConnectionRestored()
        {
            var handler = ConnectionRestored;

            if (handler != null)
                handler(this, new System.EventArgs());
        }

        private void OnConnectionShutdown(object connection, ShutdownEventArgs reason)
        {
            if (_shutdown)
                return;

            _logger.Warn("Connection to RabbitMQ has been lost.");

            StartReconnectThread();
        }

        private void StartReconnectThread()
        {
            lock (_reconnectLock)
            {
                if (_reconnectThread != null)
                    return;

                _reconnectThread = new Thread(ReconnectThreadRun)
                {
                    Name = "Reconnect"
                };

                _reconnectThread.Start();
            }
        }

        public async Task<bool> Call(string messageType, string routingKey, BasicProperties properies, byte[] body)
        {
            var tcs = new TaskCompletionSource<bool>();

            lock (_callChannelLock)
            {
                _callChannelTcsIndex.Add(properies.CorrelationId, _callChannel.NextPublishSeqNo, tcs);

                if (_exchanges.TryAdd(messageType, true))
                    _callChannel.ExchangeDeclare(messageType, "topic", _useDurableExchanges);

                _callChannel.BasicPublish(messageType, routingKey, true, properies, body);
            }

            return await tcs.Task;
        }

        public void PreparePublish(string messageType)
        {
            lock (_publishChannelLock)
            {
                if (_exchanges.TryAdd(messageType, true))
                    _publishChannel.ExchangeDeclare(messageType, "topic", _useDurableExchanges);
            }
        }

        public void Publish(string messageType, string routingKey, BasicProperties properties, byte[] body)
        {
            lock (_publishChannelLock)
            {
                if (_exchanges.TryAdd(messageType, true))
                    _publishChannel.ExchangeDeclare(messageType, "topic", _useDurableExchanges);

                _publishChannel.BasicPublish(messageType, routingKey, properties, body);
            }
        }

        public void Shutdown()
        {
            _shutdown = true;

            lock (_connectionLock)
                _connection.Close();
        }
    }
}