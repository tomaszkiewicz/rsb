using System.Collections.Generic;

namespace RSB.Diagnostics.Contracts
{
    public class GetHealthResponse
    {
        public bool Healthy { get; set; }
        public IDictionary<string, HealthState> Subsystems { get; set; }
    }
}