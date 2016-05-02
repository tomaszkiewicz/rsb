namespace RSB.Transports.RabbitMQ.QueueHandlers
{
    class BindingItem
    {
        public string ExchangeName { get; set; }
        public string RoutingKey { get; set; }
    }
}