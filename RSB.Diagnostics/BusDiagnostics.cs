using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private readonly string _runGuid;
        private readonly DateTime _runTime;

        [Obsolete("Constructor is obsolete and will be removed in future version, use UseBusDiagnostics extension instead.")]
        public BusDiagnostics(IBus bus, string moduleName, string instanceName = null)
        {
            _bus = bus;
            _moduleName = moduleName;
            _instanceName = instanceName;
            _runGuid = Guid.NewGuid().ToString().Replace("-", "");
            _runTime = DateTime.UtcNow;

            var logicalAddress = moduleName + (string.IsNullOrWhiteSpace(instanceName) ? "" : "." + instanceName);

            _bus.RegisterBroadcastHandler<DiscoveryMessage>(DiscoveryMessageHandler);
            _bus.RegisterAsyncCallHandler<GetHealthRequest, GetHealthResponse>(GetHealthHandler, logicalAddress);
        }

        private async Task<GetHealthResponse> GetHealthHandler(GetHealthRequest req)
        {
            var response = new GetHealthResponse
            {
                Subsystems = new ConcurrentDictionary<string, HealthState>()
            };

            var checkSubsystemsTasks = _subsystems.Select(s => CheckSubsystem(hs => response.Subsystems[s.Key] = hs, s.Value, req.SubsystemCheckTimeout ?? 20));

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

        private void DiscoveryMessageHandler(DiscoveryMessage discoveryMessage)
        {
            var componentInfo = new ComponentInfoMessage()
            {
                ModuleName = _moduleName,
                InstanceName = _instanceName,
                RunGuid = _runGuid,
                RunTime = _runTime,
                MachineName = Environment.MachineName,
                Username = Environment.UserName,
                DomainName = Environment.UserDomainName,
                Interactive = Environment.UserInteractive,
                Components = _subsystems.Keys.ToArray(),
                BuildTime = GetLinkerTime(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
            };

            _bus.Broadcast(componentInfo);
        }

        public void RegisterSubsystemHealthChecker(string subsystemName, Func<bool> healthCheckerFunc)
        {
            RegisterAsyncSubsystemHealthChecker(subsystemName, () =>
            {
                var tcs = new TaskCompletionSource<bool>();

                Task.Run(() => tcs.TrySetResult(healthCheckerFunc()));

                return tcs.Task;
            });
        }

        public void RegisterAsyncSubsystemHealthChecker(string subsystemName, Func<Task<bool>> healthCheckerFunc)
        {
            _subsystems[subsystemName] = healthCheckerFunc;
        }

        // http://stackoverflow.com/questions/1600962/displaying-the-build-date
        public static DateTime GetLinkerTime(Assembly assembly)
        {
            var filePath = assembly.Location;
            const int cPeHeaderOffset = 60;
            const int cLinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, cPeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + cLinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            return linkTimeUtc;
        }
    }


}