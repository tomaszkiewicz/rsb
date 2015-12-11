using System.Collections.Concurrent;

namespace RSB.Diagnostics
{
    public class ComponentHealth
    {
        public HealthState Health { get; set; }
        public ConcurrentDictionary<string, HealthState> Subsystems { get; set; }
    }
}