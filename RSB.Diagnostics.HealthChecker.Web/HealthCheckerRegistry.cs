using System.Linq;
using RabbitMQ.Client;
using RSB.Diagnostics.HealthChecker.Web.Properties;
using RSB.Interfaces;
using RSB.Transports.RabbitMQ;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;

namespace RSB.Diagnostics.HealthChecker.Web
{
    class HealthCheckerRegistry : Registry
    {
        public HealthCheckerRegistry()
        {
            Scan(x =>
            {
                x.AssembliesFromApplicationBaseDirectory();
                x.IncludeNamespace("RSB.Diagnostics.HealthChecker.Web");
            });

            var connectionFactory = new ConnectionFactory
            {
                HostName = Settings.Default.ServiceBusHost,
                UserName = Settings.Default.ServiceBusUser,
                Password = Settings.Default.ServiceBusPassword,
            };

            var bus = new Bus(new RabbitMqTransport(connectionFactory));

            For<IBus>()
                .Singleton()
                .Use(bus);

            For<HealthCheckerService>()
                .Singleton()
                .Use<HealthCheckerService>()
                .Ctor<string[]>("components")
                .Is(Settings.Default.Components.Cast<string>().ToArray())
                .Ctor<int>("interval")
                .Is(Settings.Default.Interval)
                .Ctor<double>("timeoutFactor")
                .Is(Settings.Default.TimeoutFactor)
                .OnCreation(x => x.Start());
        }
    }
}