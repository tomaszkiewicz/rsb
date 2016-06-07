using System;
using System.Threading.Tasks;
using RSB.Interfaces;

namespace RSB.Transports.Redis
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
            _typeResolver = typeResolver;
            _taskFactory = taskFactory;
        }

        public void CallDispatcher(string messageBody)
        {
            try
            {
                var obj = _serializer.Deserialize<Message<T>>(messageBody);

                var exceptionTypeName = obj.Properties.ExceptionType;

                if (!string.IsNullOrWhiteSpace(exceptionTypeName))
                {
                    try
                    {
                        var exceptionType = _typeResolver.GetType(exceptionTypeName);

                        var exceptionMessageType = typeof (Message<>).MakeGenericType(exceptionType);

                        var exception = _serializer.Deserialize(messageBody, exceptionMessageType);

                        var bodyProperty = exceptionMessageType.GetProperty("Body");

                        obj.Exception = (Exception) bodyProperty.GetValue(exception);
                    }
                    catch (Exception ex)
                    {
                        obj.Exception = ex;
                    }
                }

                StartOnTaskFactory(obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void StartOnTaskFactory(Message<T> message)
        {
            (_taskFactory ?? Task.Factory).StartNew(() => _dispatcher(message));
        }
    }
}