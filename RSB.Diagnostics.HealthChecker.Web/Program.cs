using System;
using NLog;
using Topshelf;

namespace RSB.Diagnostics.HealthChecker.Web
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static void Main()
        {
            HostFactory.Run(x =>
            {
                x.SetServiceName("RSB.Diagnostics.HealthChecker.Web");
                x.SetDisplayName("RSB.Diagnostics.HealthChecker.Web");
                x.SetDescription("Service that exposes HTTP endpoint to check health of system.");

                x.StartAutomatically();

                x.UseNLog();

                x.Service<HealthCheckerServer>(service =>
                {
                    service.ConstructUsing(srv => InitializeHealtCheckerServiceService());

                    service.WhenStarted(srv => srv.Start());
                    service.WhenStopped(srv => srv.Stop());
                });
            });
        }

        private static HealthCheckerServer InitializeHealtCheckerServiceService()
        {
            Logger.Info("Starting RSB.Diagnostics.HealthChecker.Web service...");
            
            Logger.Debug("Initializing service...");

            System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            return new HealthCheckerServer();
        }
    }
}
