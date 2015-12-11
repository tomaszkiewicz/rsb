namespace RSB.Diagnostics.HealthChecker.Web.Entities
{
    public enum State
    {
        Unknown,
        Offline,
        Timeout,
        Healthy,
        Unhealthy,
        InvalidComponent
    }
}