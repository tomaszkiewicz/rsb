using System;
using System.Collections.Concurrent;

namespace RSB.Diagnostics
{
    public class ComponentHealth
    {
        public string ComponentName { get; set; }
        public DateTime? LastCheckTime { get; set; }
        public HealthState Health { get; set; }
        public ConcurrentDictionary<string, HealthState> Subsystems { get; set; }
        public DateTime? LastResponseTime { get; set; }
        public DateTime? LastFailureTime { get; set; }
        public TimeSpan ResponseLatency { get; set; }
    }
}