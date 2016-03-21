using System.Linq;
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
            
            var bus = new Bus(RabbitMqTransport.FromConfigurationFile());

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