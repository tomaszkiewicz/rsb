using System;
using RSB.Interfaces;

namespace RSB.Diagnostics
{
    public static class BusDiagnosticsExtensions
    {
        public static void UseBusDiagnostics(this IBus bus, string moduleName, string instanceName = null)
        {
            // ReSharper disable once ObjectCreationAsStatement
            new BusDiagnostics(bus, moduleName, instanceName);
        }

        public static void UseBusDiagnostics(this IBus bus, string moduleName, string instanceName, Action<BusDiagnostics> configureDiagnostics)
        {
            var diagnostics = new BusDiagnostics(bus, moduleName, instanceName);

            configureDiagnostics(diagnostics);
        }

        public static void UseBusDiagnostics(this IBus bus, string moduleName, Action<BusDiagnostics> configureDiagnostics)
        {
            var diagnostics = new BusDiagnostics(bus, moduleName);

            configureDiagnostics(diagnostics);
        }
    }
}