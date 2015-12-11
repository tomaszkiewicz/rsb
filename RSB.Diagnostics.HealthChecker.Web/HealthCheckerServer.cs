using System;
using Microsoft.Owin.Hosting;

namespace RSB.Diagnostics.HealthChecker.Web
{
    class HealthCheckerServer
    {
        private IDisposable _server;
        private readonly string _baseAddress;

        public HealthCheckerServer(string baseAddress = "http://+/")
        {
            _baseAddress = baseAddress;
        }
        
        public void Start()
        {
            _server = WebApp.Start<Startup>(_baseAddress);
        }

        public void Stop()
        {
            _server.Dispose();
        }
    }
}
