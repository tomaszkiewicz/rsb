namespace RSB.Diagnostics
{
    public enum HealthState
    {
        Unknown,
        Healthy,
        Unhealthy,
        Timeout,
        Offline,
        Exception,
        NotConnected
    }
}