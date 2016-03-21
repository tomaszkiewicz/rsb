using System;
using RSB;
using RSB.Diagnostics;
using RSB.Transports.RabbitMQ;

namespace SampleDiscoverableModule
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = new Bus(RabbitMqTransport.FromConfigurationFile());

            var diagnostics = new BusDiagnostics(bus,"SampleDiscoverableModule");

            diagnostics.RegisterSubsystemHealthChecker("sampleSubsystem1", () => true);

            Console.ReadLine();
        }
    }
}