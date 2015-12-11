using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RSB.Exceptions;
using RSB.Serialization;

namespace RSB.Transports.RabbitMQ
{
    class TaskFactoryInvokeReceiveAction<T> : ITaskFactoryInvokeReceiveAction where T : new()
    {
        private readonly Action<Message<T>> _dispatcher;
        private readonly ISerializer _serializer;
        private readonly TaskFactory _taskFactory;
        private readonly TypeResolver _typeResolver;

        public TaskFactoryInvokeReceiveAction(Action<Message<T>> dispatcher, ISerializer serializer, TypeResolver typeResolver, TaskFactory taskFactory = null)
        {
            _dispatcher = dispatcher;
            _serializer = serializer;
            _taskFactory = taskFactory;
            _typeResolver = typeResolver;
        }

        public void CallDispatcher(BasicDeliverEventArgs item)
        {
            if (CheckIfTypedException(item))
            {
                DeserializeException(item);

                return;
            }

            if (CheckIfGenericException(item))
            {
                DispatchException(item, new RemoteException(GetHeaderValue(item, "exception")));

                return;
            }

            try
            {
                var obj = _serializer.Deserialize<T>(item.Body);

                StartOnTaskFactory(item.ToMessage(obj));
            }
            catch (SerializationException ex)
            {
                DispatchException(item, ex);
            }
        }

        private void DeserializeException(BasicDeliverEventArgs item)
        {
            Exception exception;

            try
            {
                var type = _typeResolver.GetType(GetHeaderValue(item, "exceptionType"));

                exception = (Exception)_serializer.Deserialize(item.Body, type);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            DispatchException(item, exception);
        }

        private void DispatchException(BasicDeliverEventArgs item, Exception exception)
        {
            var faultedObj = item.ToMessage(default(T));

            faultedObj.Exception = exception;

            StartOnTaskFactory(faultedObj);
        }

        private void StartOnTaskFactory(Message<T> message)
        {
            (_taskFactory ?? Task.Factory).StartNew(() => _dispatcher(message));
        }

        string GetHeaderValue(BasicDeliverEventArgs item, string headerName)
        {
            return System.Text.Encoding.UTF8.GetString((byte[])item.BasicProperties.Headers[headerName]);
        }

        bool CheckIfTypedException(BasicDeliverEventArgs item)
        {
            return item.BasicProperties.Headers != null && item.BasicProperties.Headers.ContainsKey("exceptionType");
        }
        bool CheckIfGenericException(BasicDeliverEventArgs item)
        {
            return item.BasicProperties.Headers != null && item.BasicProperties.Headers.ContainsKey("exception");
        }
    }
}