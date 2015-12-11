using System.Collections.Generic;

namespace RSB.Diagnostics.HealthChecker.Web.Entities
{
    public class HealthResponse
    {
        public State State { get; set; }
        public IDictionary<string, HealthState> Subsystems { get; set; }
        public string Component { get; set; }
    }
}