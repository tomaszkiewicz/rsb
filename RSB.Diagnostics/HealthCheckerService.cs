using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private volatile bool _checking = false;

        private void Run(object state)
        {
            if (_checking)
                return;

            try
            {
                _checking = true;

                var checkTasks = _components.Select(CheckComponentHealth);

                try
                {
                    Task.WaitAll(checkTasks.ToArray());
                }
                catch (Exception ex)
                { }

                RaiseCheckCompleted();
            }
            finally
            {
                _checking = false;
            }
        }

        private void RaiseCheckCompleted()
        {
            var handler = CheckCompleted;

            if (handler != null)
                CheckCompleted(this, new System.EventArgs());
        }

        private async Task CheckComponentHealth(string componentName)
        {
            HealthState state;
            IDictionary<string, HealthState> subsystems = null;

            try
            {
                var response = await CallGetHealth(componentName);

                state = response.Healthy ? HealthState.Healthy : HealthState.Unhealthy;

                subsystems = response.Subsystems;
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

            var componentHealth = _componentsHealths.GetOrAdd(componentName, c => new ComponentHealth()
            {
                Subsystems = new ConcurrentDictionary<string, HealthState>()
            });

            componentHealth.Health = state;

            if (subsystems != null)
            {
                foreach (var subsystem in subsystems)
                    componentHealth.Subsystems[subsystem.Key] = subsystem.Value;

                foreach (var subsystemName in componentHealth.Subsystems.Keys.Except(subsystems.Keys))
                    componentHealth.Subsystems[subsystemName] = HealthState.Unknown;
            }
            else
            {
                foreach (var subsystem in componentHealth.Subsystems)
                    componentHealth.Subsystems[subsystem.Key] = state;
            }
        }

        private Task<GetHealthResponse> CallGetHealth(string componentName)
        {
            return _bus.Call<GetHealthRequest, GetHealthResponse>(new GetHealthRequest()
            {
                SubsystemCheckTimeout = GetTimeout(GetTimeout(_interval))
            }, componentName, GetTimeout(_interval)).TimeoutAfter(GetTimeout(_interval));
        }

        public IDictionary<string, ComponentHealth> GetComponentsHealth()
        {
            return _componentsHealths.ToDictionary(k => k.Key, v => v.Value);
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