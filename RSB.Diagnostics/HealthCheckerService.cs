using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RSB.Diagnostics.Contracts;
using RSB.Exceptions;
using RSB.Extensions;
using RSB.Interfaces;

namespace RSB.Diagnostics
{
    public class HealthCheckerService : IDisposable
    {
        private readonly IBus _bus;
        private readonly string[] _components;
        private readonly int _interval;
        private readonly double _timeoutFactor;

        readonly ConcurrentDictionary<string, ComponentHealth> _componentsHealths = new ConcurrentDictionary<string, ComponentHealth>();
        private Timer _timer;

        public event EventHandler CheckCompleted;

        public HealthCheckerService(IBus bus, string[] components, int interval, double timeoutFactor = 0.9d)
        {
            _bus = bus;
            _components = components;
            _interval = interval;
            _timeoutFactor = timeoutFactor;

            if (_timeoutFactor > 1 || _timeoutFactor <= 0)
                throw new ArgumentOutOfRangeException("timeoutFactor", "timeoutFactor has to be between 0 and 1.");

            if (GetTimeout(GetTimeout(_interval)) == 0)
                throw new ArgumentOutOfRangeException("interval", "Too low interval for specified timeoutFactor.");

            foreach (var componentName in components.OrderBy(s => s))
            {
                var name = componentName;

                _componentsHealths.GetOrAdd(componentName, c => new ComponentHealth()
                {
                    ComponentName = name,
                    Subsystems = new ConcurrentDictionary<string, HealthState>()
                });
            }
        }

        public void Start()
        {
            _timer = new Timer(Run, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(_interval));
        }

        public void Stop()
        {
            if (_timer == null)
                return;

            _timer.Dispose();
            _timer = null;
        }
        

        private void Run(object stateObj)
        {
            foreach (var component in _components)
                CheckComponent(component);
        }

        private async Task CheckComponent(string componentName)
        {
            var componentHealth = _componentsHealths.GetOrAdd(componentName, c => new ComponentHealth()
            {
                ComponentName = componentName,
                Subsystems = new ConcurrentDictionary<string, HealthState>()
            });

            componentHealth.LastCheckTime = DateTime.UtcNow;

            var sw = Stopwatch.StartNew();

            await _bus.Call<GetHealthRequest, GetHealthResponse>(new GetHealthRequest()
            {
                SubsystemCheckTimeout = GetTimeout(GetTimeout(_interval))
            }, componentName, GetTimeout(_interval)).TimeoutAfter(_interval)
                .ContinueWith(async t =>
                {
                    HealthState state;
                    GetHealthResponse response = null;

                    try
                    {
                        response = await t;

                        state = response.Healthy ? HealthState.Healthy : HealthState.Unhealthy;

                        componentHealth.LastResponseTime = DateTime.UtcNow;

                        sw.Stop();

                        componentHealth.ResponseLatency = sw.Elapsed;
                    }
                    catch (MessageReturnedException)
                    {
                        state = HealthState.Offline;
                    }
                    catch (TimeoutException)
                    {
                        state = HealthState.Timeout;
                    }
                    catch (NotConnectedException)
                    {
                        state = HealthState.NotConnected;
                    }
                    catch (Exception)
                    {
                        state = HealthState.Unknown;
                    }

                    if (state != HealthState.Healthy && state != HealthState.Unhealthy && (componentHealth.Health == HealthState.Healthy || componentHealth.Health == HealthState.Unhealthy))
                        componentHealth.LastFailureTime = DateTime.UtcNow;

                    componentHealth.Health = state;

                    if (response != null && response.Subsystems != null)
                    {
                        foreach (var kvp in response.Subsystems)
                            componentHealth.Subsystems[kvp.Key] = kvp.Value;

                        foreach (var subsystemName in componentHealth.Subsystems.Keys.Except(response.Subsystems.Keys))
                            componentHealth.Subsystems[subsystemName] = HealthState.Unknown;
                    }
                    else
                    {
                        foreach (var subsystemName in componentHealth.Subsystems.Keys)
                            componentHealth.Subsystems[subsystemName] = HealthState.Unknown;
                    }

                    RaiseCheckCompleted();
                });
        }

        private void RaiseCheckCompleted()
        {
            var handler = CheckCompleted;

            if (handler != null)
                CheckCompleted(this, new System.EventArgs());
        }

        public IEnumerable<ComponentHealth> GetComponentsHealth()
        {
            return _componentsHealths.Values;
        }

        private int GetTimeout(int interval)
        {
            return (int)Math.Floor(interval * _timeoutFactor);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}