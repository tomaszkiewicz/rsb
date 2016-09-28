using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RSB.EventArgs;
using RSB.Exceptions;
using RSB.Extensions;
using RSB.Interfaces;
using Exception = System.Exception;

namespace RSB
{
    public class Bus : IBus
    {
        public event EventHandler<BusExceptionEventArgs> DeserializationError;
        public event EventHandler<BusExceptionEventArgs> ExecutionError;
        public event EventHandler<MessageMalformedEventArgs> MessageMalformed;

        private readonly ITransport _transport;

        private readonly ConcurrentDictionary<string, RpcCallHandler> _rpcCallHandlers = new ConcurrentDictionary<string, RpcCallHandler>();
        private readonly string _rpcCallbackLogicalAddressTemplate = "rpc-{0}-" + Guid.NewGuid();

        public Bus(ITransport transport)
        {
            _transport = transport;
        }

        public void Dispose()
        {
            Shutdown();
        }

        public void Shutdown()
        {
            _transport.Shutdown();
        }

        public void Enqueue<T>(T obj, string logicalAddress = "", int expirationSeconds = 0)
        {
            CheckConnection();

            var properties = new MessageProperties
            {
                Type = GetMessageType<T>(),
                ExpirationMilliseconds = expirationSeconds * 1000
            };

            _transport.Enqueue(logicalAddress, properties, obj);
        }

        public void Broadcast<T>(T obj, string logicalAddress = "", int expirationSeconds = 0)
        {
            CheckConnection();

            var properties = new MessageProperties
            {
                Type = GetMessageType<T>(),
                ExpirationMilliseconds = expirationSeconds * 1000
            };

            _transport.Broadcast(logicalAddress, properties, obj);
        }

        public async Task<TResponse> Call<TRequest, TResponse>(TRequest obj, string logicalAddress = "", int timeoutSeconds = 60) where TResponse : new()
        {
            CheckConnection();

            var tcs = new TaskCompletionSource<object>();
            var rpcLogicalAddress = string.Format(_rpcCallbackLogicalAddressTemplate, GetMessageType<TResponse>());

            var correlationId = Guid.NewGuid().ToString();

            var handler = new RpcCallHandler
            {
                ResponseType = typeof(TResponse),
                TaskCompletion = tcs,
            };

            _rpcCallHandlers[correlationId] = handler;

            _transport.Subscribe<TResponse>(RpcCallDispatcher, rpcLogicalAddress, rpcLogicalAddress);

            var properties = new MessageProperties
            {
                Type = GetMessageType<TRequest>(),
                ReplyTo = rpcLogicalAddress,
                CorrelationId = correlationId,
                ExpirationMilliseconds = timeoutSeconds * 1000
            };

            try
            {
                await _transport.Call(logicalAddress, properties, obj).TimeoutAfter(5);
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException("Failed to call - timeout", ex);
            }

            try
            {
                return (TResponse)await tcs.Task.TimeoutAfter(timeoutSeconds);
            }
            catch (TimeoutException)
            {
                _rpcCallHandlers.TryRemove(correlationId, out handler);

                throw;
            }
        }

        public void PrepareEnqueue<T>() where T : new()
        {
            _transport.Prepare<T>();
        }

        public void PrepareBroadcast<T>() where T : new()
        {
            _transport.Prepare<T>();
        }

        public void PrepareCall<TRequest, TResponse>()
            where TRequest : new()
            where TResponse : new()
        {
            _transport.Prepare<TRequest>();
            _transport.Prepare<TResponse>();
        }

        public void RegisterQueueHandler<T>(Action<T> handler, string logicalAddress = "", TaskFactory taskFactory = null) where T : new()
        {
            RegisterAsyncQueueHandler<T>(msg =>
            {
                var tcs = new TaskCompletionSource<bool>();

                Task.Run(() =>
                {
                    try
                    {
                        handler(msg);

                        tcs.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                return tcs.Task;
            }, logicalAddress, taskFactory);
        }

        public void RegisterAsyncQueueHandler<T>(Func<T, Task> handler, string logicalAddress = "", TaskFactory taskFactory = null) where T : new()
        {
            _transport.Prepare<T>();

            Action<Message<T>> dispatcher = async message =>
            {
                await HandleExecutionErrors(message, async () =>
                {
                    await handler(message.Body);
                });
            };

            var listenAddress = $"queue-{GetMessageType<T>()}-{logicalAddress}";

            _transport.Subscribe(dispatcher, logicalAddress, listenAddress, taskFactory);
        }

        public void RegisterBroadcastHandler<T>(Action<T> handler, string logicalAddress = "", TaskFactory taskFactory = null) where T : new()
        {
            RegisterAsyncBroadcastHandler<T>(msg =>
            {
                var tcs = new TaskCompletionSource<bool>();

                Task.Run(() =>
                {
                    try
                    {
                        handler(msg);

                        tcs.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                return tcs.Task;
            }, logicalAddress, taskFactory);
        }

        public void RegisterAsyncBroadcastHandler<T>(Func<T, Task> handler, string logicalAddress = "", TaskFactory taskFactory = null) where T : new()
        {
            _transport.Prepare<T>();

            Action<Message<T>> dispatcher = async message =>
            {
                await HandleExecutionErrors(message, async () =>
                {
                    await handler(message.Body);
                });
            };

            _transport.Subscribe(dispatcher, logicalAddress, "broadcast-" + Guid.NewGuid(), taskFactory);
        }

        public void RegisterCallHandler<TRequest, TResponse>(Func<TRequest, TResponse> handler, string logicalAddress = "", TaskFactory taskFactory = null)
            where TResponse : new()
            where TRequest : new()
        {
            RegisterAsyncCallHandler<TRequest, TResponse>(req =>
            {
                var tcs = new TaskCompletionSource<TResponse>();

                Task.Run(() =>
                {
                    try
                    {
                        tcs.TrySetResult(handler(req));
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                return tcs.Task;
            }, logicalAddress, taskFactory);
        }

        public void RegisterAsyncCallHandler<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler, string logicalAddress = "", TaskFactory taskFactory = null)
            where TResponse : new()
            where TRequest : new()
        {
            _transport.Prepare<TRequest>();
            _transport.Prepare<TResponse>();

            Action<Message<TRequest>> dispatcher = async message =>
            {
                var properties = message.Properties;

                var responseObj = default(TResponse);

                var exception = await HandleExecutionErrors(message, async () =>
                {
                    responseObj = await handler(message.Body);
                });

                if (string.IsNullOrWhiteSpace(properties.ReplyTo))
                {
                    MessageMalformed?.Invoke(this, new MessageMalformedEventArgs("Message has no ReplyTo field set, so RSB cannot respond."));

                    return;
                }

                var responseProperties = new MessageProperties
                {
                    Type = GetMessageType<TResponse>(),
                    CorrelationId = properties.CorrelationId,
                };

                if (exception != null)
                {
                    responseProperties.ExceptionType = exception.GetType().Name;

                    _transport.Enqueue(properties.ReplyTo, responseProperties, exception);

                    return;
                }

                _transport.Enqueue(properties.ReplyTo, responseProperties, responseObj);
            };

            var listenAddress = $"call-{GetMessageType<TRequest>()}-{logicalAddress}";

            _transport.Subscribe(dispatcher, logicalAddress, listenAddress, taskFactory);
        }

        private void CheckConnection()
        {
            if (!_transport.IsConnected)
                throw new NotConnectedException();
        }

        private void RpcCallDispatcher<T>(Message<T> message)
        {
            var properties = message.Properties;
            var correlationId = properties.CorrelationId;

            RpcCallHandler handler;

            if (correlationId == null)
            {
                MessageMalformed?.Invoke(this, new MessageMalformedEventArgs($"Got null correlationId for response message type {properties.Type}"));

                return;
            }

            if (!_rpcCallHandlers.TryRemove(correlationId, out handler))
            {
                MessageMalformed?.Invoke(this, new MessageMalformedEventArgs("Got unmapped correlationId ({correlationId}) for response message type {properties.Type}"));

                return;
            }

            var tcs = handler.TaskCompletion;

            if (message.Exception != null)
            {
                tcs.SetException(message.Exception);
                return;
            }

            tcs.SetResult(message.Body);
        }

        private async Task<Exception> HandleExecutionErrors<T>(Message<T> message, Func<Task> action)
        {
            try
            {
                await action();

                return null;
            }
            catch (Exception ex)
            {
                if (ex is SerializationException)
                {
                    RaiseBusExceptionEvent(DeserializationError, ex, message);
                }
                else
                {
                    RaiseBusExceptionEvent(ExecutionError, ex, message);
                }

                return ex;
            }
        }

        private void RaiseBusExceptionEvent<T>(EventHandler<BusExceptionEventArgs> raiseEventHandler, Exception ex, Message<T> message)
        {
            raiseEventHandler?.Invoke(this, new BusExceptionEventArgs()
            {
                Exception = ex,
                Message = message,
                MessageType = typeof(T)
            });
        }

        private static string GetMessageType<T>()
        {
            return typeof(T).Name;
        }
    }
}