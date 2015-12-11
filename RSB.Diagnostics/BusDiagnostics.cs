using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using RSB.Diagnostics.Contracts;
using RSB.Extensions;
using RSB.Interfaces;

namespace RSB.Diagnostics
{
    public class BusDiagnostics
    {
        private readonly IBus _bus;
        private readonly string _moduleName;
        private readonly string _instanceName;

        private readonly ConcurrentDictionary<string, Func<Task<bool>>> _subsystems = new ConcurrentDictionary<string, Func<Task<bool>>>();
        private string _runGuid;

        public BusDiagnostics(IBus bus, string moduleName, string instanceName = null)
        {
            _bus = bus;
            _moduleName = moduleName;
            _instanceName = instanceName;
            _runGuid = Guid.NewGuid().ToString().Replace("-", "");

            _bus.RegisterAsyncBroadcastHandler<DiscoveryMessage>(DiscoveryMessageHandler);

            var logicalAddress = moduleName + (string.IsNullOrWhiteSpace(instanceName) ? "" : "." + instanceName);

            _bus.RegisterAsyncCallHandler<GetHealthRequest, GetHealthResponse>(GetHealthHandler, logicalAddress);
        }

        private async Task<GetHealthResponse> GetHealthHandler(GetHealthRequest req)
        {
            var response = new GetHealthResponse
            {
                Subsystems = new ConcurrentDictionary<string, HealthState>()
            };
            
            var checkSubsystemsTasks = _subsystems.Select(s => CheckSubsystem(hs => response.Subsystems[s.Key] = hs, s.Value, req.SubsystemCheckTimeout));

            await Task.WhenAll(checkSubsystemsTasks);

            response.Healthy = response.Subsystems.Values.All(v => v == HealthState.Healthy);

            return response;
        }

        private async Task CheckSubsystem(Action<HealthState> setHealthState, Func<Task<bool>> checkFunc, int timeout)
        {
            try
            {
                var result = await checkFunc().TimeoutAfter(timeout);

                setHealthState(result ? HealthState.Healthy : HealthState.Unhealthy);
            }
            catch (TimeoutException)
            {
                setHealthState(HealthState.Timeout);
            }
            catch (Exception)
            {
                setHealthState(HealthState.Exception);
            }
        }

        private Task DiscoveryMessageHandler(DiscoveryMessage discoveryMessage)
        {
            throw new NotImplementedException();
        }

        public void RegisterSubsystemHealthChecker(string subsystemName, Func<bool> healthCheckerFunc)
        {
            RegisterAsyncSubsystemHealthChecker(subsystemName, () => Task.FromResult(healthCheckerFunc()));
        }

        public void RegisterAsyncSubsystemHealthChecker(string subsystemName, Func<Task<bool>> healthCheckerFunc)
        {
            _subsystems[subsystemName] = healthCheckerFunc;
        }
    }
}