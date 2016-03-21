using System;
using System.Linq;
using System.Threading;
using RSB.Diagnostics.Contracts;
using RSB.Transports.RabbitMQ;
using RSB.Transports.RabbitMQ.Settings;

namespace RSB.Diagnostics.HealthChecker
{
    internal static class Program
    {
        private static Bus _bus;
        private static HealthCheckerService _healthCheckerService;
        private static ConsoleColor _baseColor;
        private static object _printLock = new object();
        private static bool _changed;
        private static Timer _timer;

        private static void Main(string[] args)
        {
            _baseColor = Console.ForegroundColor;

            var settings = RabbitMqTransportSettings.FromConfigurationFile();

            Console.WriteLine("Connecting to service bus {0}...", settings.Hostname);

            _bus = new Bus(new RabbitMqTransport(settings));

            var components = Properties.Settings.Default.Components.Cast<string>().ToArray();

            _healthCheckerService = new HealthCheckerService(_bus, components, Properties.Settings.Default.CheckInterval);

            _healthCheckerService.CheckCompleted += OnCheckCompleted;

            _healthCheckerService.Start();

            Console.WriteLine("Performing initial health check...");

            _timer = new Timer(obj => PrintHealth(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            while (Console.ReadLine() != "exit")
            { }

            _healthCheckerService.Stop();
        }

        private static void OnCheckCompleted(object sender, System.EventArgs e)
        {
            _changed = true;
        }

        private static void PrintHealth()
        {
            lock (_printLock)
            {
                const int componentColumnWidth = 50;
                const int stateColumnWidth = 15;
                const int lastCheckWidth = 15;
                const int lastFailureWidth = 20;
                const int responseLatencyWidth = 20;

                var delimeter = "";

                for (var i = 0; i < componentColumnWidth + stateColumnWidth + lastFailureWidth + lastCheckWidth + responseLatencyWidth; i++)
                    delimeter += "-";

                var componentsHealths = _healthCheckerService.GetComponentsHealth().OrderBy(c => c.ComponentName);

                Console.Clear();
                Console.WriteLine();
                Console.WriteLine(FormatToWidth("  Component", componentColumnWidth) +
                                  FormatToWidth("State", stateColumnWidth) +
                                  FormatToWidth("Last check", lastCheckWidth) +
                                  FormatToWidth("Response latency", responseLatencyWidth) +
                                  FormatToWidth("Last failure", lastFailureWidth));
                Console.WriteLine(delimeter);

                var first = true;

                foreach (var componentHealth in componentsHealths)
                {
                    if (!first)
                        Console.WriteLine();
                    else
                        first = false;

                    Console.Write(FormatToWidth("  " + componentHealth.ComponentName, componentColumnWidth));

                    PrintHealth(componentHealth.Health, stateColumnWidth);

                    Console.Write(FormatToWidth(FormatAgo(componentHealth.LastCheckTime), lastCheckWidth));
                    Console.Write(FormatToWidth(FormatTimespan(componentHealth.ResponseLatency), responseLatencyWidth));
                    Console.Write(FormatToWidth(FormatTime(componentHealth.LastFailureTime), lastFailureWidth));

                    Console.WriteLine();

                    foreach (var subsystemKvp in componentHealth.Subsystems)
                    {
                        Console.Write(FormatToWidth("   -> " + subsystemKvp.Key, componentColumnWidth));

                        PrintHealth(subsystemKvp.Value, stateColumnWidth);

                        Console.WriteLine();
                    }
                }

                Console.WriteLine(delimeter);
            }
        }

        private static string FormatTimespan(TimeSpan? responseLatency)
        {
            if (responseLatency == null || (int)responseLatency.Value.TotalMilliseconds == 0)
                return "N/A";

            return (int)responseLatency.Value.TotalMilliseconds + "ms";
        }

        private static string FormatAgo(DateTime? time)
        {
            if (time == null)
                return "Never";

            var secondsAgo = (int)(DateTime.UtcNow - (DateTime)time).TotalSeconds;

            if (secondsAgo == 0)
                return "Just now";

            return secondsAgo + "s ago";
        }

        private static string FormatTime(DateTime? time)
        {
            if (time == null)
                return "Never";

            return time.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private static void PrintHealth(HealthState state, int stateColumnWidth)
        {
            switch (state)
            {
                case HealthState.Healthy:
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write(FormatToWidth("Healthy", stateColumnWidth));
                    }
                    break;

                case HealthState.Unhealthy:
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write(FormatToWidth("Unhealthy", stateColumnWidth));
                    }
                    break;

                case HealthState.Unknown:
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(FormatToWidth("Unknown", stateColumnWidth));
                    }
                    break;

                default:
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(FormatToWidth(state.ToString(), stateColumnWidth));
                    }
                    break;
            }

            Console.ForegroundColor = _baseColor;
        }

        static string FormatToWidth(string str, int width, bool toRight = false, char fillChar = ' ')
        {
            if (str.Length > width)
                str = str.Substring(0, width);

            var add = width - str.Length;

            for (var i = 0; i < add; i++)
                if (toRight)
                    str = fillChar + str;
                else
                    str += fillChar;

            return str;
        }
    }
}
