using System;
using System.Threading;
using RSB.Transports.RabbitMQ;

namespace RSB.Diagnostics.DiscoveryClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = new Bus(RabbitMqTransport.FromConfigurationFile());

            var discoveryClient = new BusDiscoveryClient(bus);

            Thread.Sleep(2000);

            Console.WriteLine("Discovering...");

            var components = discoveryClient.DiscoverComponents(5).Result;

            Console.WriteLine("Discovery complete.");
            Console.Clear();

            foreach (var component in components)
            {
                Console.WriteLine("{0}.{1}.{2}", component.ModuleName, string.IsNullOrWhiteSpace(component.InstanceName) ? component.InstanceName : "[no instance name]", component.RunGuid);
                Console.WriteLine("\tMachine:\t{0}", component.MachineName);
                Console.WriteLine("\tUsername:\t{0}", component.Username);
                Console.WriteLine("\tDomain name:\t{0}", component.DomainName);
                Console.WriteLine("\tInteractive:\t{0}", component.Interactive);
                Console.WriteLine("\tBuilt time:\t{0}", component.BuildTime);
                
                Console.WriteLine();
            }

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }
    }
}