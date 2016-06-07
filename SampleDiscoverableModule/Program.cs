using System;
using System.Threading;
using RSB;
using RSB.Diagnostics;
using RSB.Transports.RabbitMQ;

namespace SampleDiscoverableModule
{
    class Program
    {
        static void Main()
        {
            var bus = new Bus(RabbitMqTransport.FromConfigurationFile());

            bus.UseBusDiagnostics("SampleDiscoverableModule", diagnostics =>
            {
                diagnostics.RegisterSubsystemHealthChecker("sampleSubsystem1", () =>
                {
                    Thread.Sleep(6000);

                    return true;
                });
            });

            Console.ReadLine();
        }
    }
}