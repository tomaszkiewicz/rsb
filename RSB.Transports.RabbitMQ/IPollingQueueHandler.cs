using RabbitMQ.Client;

namespace RSB.Transports.RabbitMQ
{
    public interface IPollingQueueHandler
    {
        void Ack(BasicGetResult item);
        void BindToExchange(string exchangeName, string routingKey = "#");
        BasicGetResult GetItem();
    }
}