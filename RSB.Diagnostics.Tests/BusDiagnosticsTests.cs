using System.Threading;
using NUnit.Framework;
using RSB.Interfaces;
using RSB.Transports.RabbitMQ;

namespace RSB.Diagnostics.Tests
{
    [TestFixture]
    public class BusDiagnosticsTests
    {
        private IBus _busServer1;
        private IBus _busServer2;
        private IBus _busClient;

        private volatile bool _busServer1Subsystem1Health = true;
        private volatile bool _busServer1Subsystem2Health = true;
        private volatile bool _busServer2Subsystem1Health = true;
        private volatile bool _busServer2Subsystem2Health = true;
        private BusDiscoveryClient _discoveryClient;

        [SetUp]
        public void Init()
        {
            _busServer1 = new Bus(RabbitMqTransport.FromConfigurationFile());

            var diagnostics1 = new BusDiagnostics(_busServer1, "Module1", "Instance1");

            diagnostics1.RegisterSubsystemHealthChecker("subsystem1", () => _busServer1Subsystem1Health);
            diagnostics1.RegisterSubsystemHealthChecker("subsystem2", () => _busServer1Subsystem2Health);

            _busServer2 = new Bus(RabbitMqTransport.FromConfigurationFile());

            var diagnostics2 = new BusDiagnostics(_busServer2, "Module2", "Instance2");

            diagnostics2.RegisterSubsystemHealthChecker("subsystem1", () => _busServer2Subsystem1Health);
            diagnostics2.RegisterSubsystemHealthChecker("subsystem2", () => _busServer2Subsystem2Health);

            _busClient = new Bus(RabbitMqTransport.FromConfigurationFile());

            _discoveryClient = new BusDiscoveryClient(_busClient);

            Thread.Sleep(3000);
        }

        [TearDown]
        public void Deinit()
        {
            _busServer1.Shutdown();
            _busServer2.Shutdown();
            _busClient.Shutdown();
        }

        [Test]
        public async void TestDiscovery()
        {
            await _discoveryClient.DiscoverComponents(5);
            
        }
    }
}