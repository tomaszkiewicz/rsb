using RabbitMQ.Client.Events;

namespace RSB.Transports.RabbitMQ
{
    internal interface ITaskFactoryInvokeReceiveAction
    {
        void CallDispatcher(BasicDeliverEventArgs item);
    }
}