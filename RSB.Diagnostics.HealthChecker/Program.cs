using System;
using System.Linq;
using RSB.Transports.RabbitMQ;

namespace RSB.Diagnostics.HealthChecker
{
    internal static class Program
    {
        private static Bus _bus;
        private static HealthCheckerService _healthCheckerService;
        private static ConsoleColor _baseColor;

        private static void Main(string[] args)
        {
            _baseColor = Console.ForegroundColor;

            Console.WriteLine("Connecting to service bus {0}...", Properties.Settings.Default.ServiceBusHost);

            _bus = new Bus(new RabbitMqTransport(Properties.Settings.Default.ServiceBusHost, Properties.Settings.Default.ServiceBusUser, Properties.Settings.Default.ServiceBusPassword));

            var components = Properties.Settings.Default.Components.Cast<string>().ToArray();

            _healthCheckerService = new HealthCheckerService(_bus, components, Properties.Settings.Default.CheckInterval);

            _healthCheckerService.CheckCompleted += OnCheckCompleted;

            _healthCheckerService.Start();

            Console.WriteLine("Performing initial health check...");

            while (Console.ReadLine() != "exit")
            { }

            _healthCheckerService.Stop();
        }

        private static void OnCheckCompleted(object sender, System.EventArgs e)
        {
            const int componentColumnWidth = 50;
            const int stateColumnWidth = 30;

            var delimeter = "";

            for (var i = 0; i < componentColumnWidth + stateColumnWidth; i++)
                delimeter += "-";

            var componentsHealths = _healthCheckerService.GetComponentsHealth();

            Console.Clear();
            Console.WriteLine();
            Console.WriteLine(FormatToWidth("  Component", componentColumnWidth) + FormatToWidth("State", stateColumnWidth));
            Console.WriteLine(delimeter);

            foreach (var componentHealth in componentsHealths)
            {
                var componentName = componentHealth.Key;
                var componentState = componentHealth.Value;

                Console.Write(FormatToWidth("  " + componentName, componentColumnWidth));

                PrintHealth(componentState.Health, stateColumnWidth);

                foreach (var subsystemKvp in componentState.Subsystems)
                {
                    Console.Write(FormatToWidth("   -> " + subsystemKvp.Key, componentColumnWidth));

                    PrintHealth(subsystemKvp.Value, stateColumnWidth);
                }
            }

            Console.WriteLine(delimeter);
            Console.WriteLine();
            Console.WriteLine("  Check time: {0}", DateTime.Now);
            Console.WriteLine();
        }

        private static void PrintHealth(HealthState state, int stateColumnWidth)
        {
            switch (state)
            {
                case HealthState.Healthy:
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine(FormatToWidth("Healthy", stateColumnWidth));
                    }
                    break;

                case HealthState.Unhealthy:
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(FormatToWidth("Unhealthy", stateColumnWidth));
                    }
                    break;

                default:
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(FormatToWidth(state.ToString(), stateColumnWidth));
                    }
                    break;
            }

            Console.ForegroundColor = _baseColor;
        }

        static string FormatToWidth(string str, int width, bool toRight = false)
        {
            if (str.Length > width)
                str = str.Substring(0, width);

            var add = width - str.Length;

            for (var i = 0; i < add; i++)
                if (toRight)
                    str = " " + str;
                else
                    str += " ";

            return str;
        }
    }
}
