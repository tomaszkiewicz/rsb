using RabbitMQ.Client.Events;

namespace RSB.Transports.RabbitMQ
{
    public interface IBlockingQueueHandler
    {
        BasicDeliverEventArgs GetItem(int timeoutMilliseconds = 5000);
        void BindToExchange(string exchangeName, string routingKey = "#");
        void Start();
        void Stop();
    }
}