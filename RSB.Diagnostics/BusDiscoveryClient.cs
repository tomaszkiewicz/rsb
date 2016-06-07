using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RSB.Diagnostics.Contracts;
using RSB.Interfaces;

namespace RSB.Diagnostics
{
    public class BusDiscoveryClient
    {
        private readonly IBus _bus;

        readonly ConcurrentDictionary<string, ComponentInfoMessage> _components = new ConcurrentDictionary<string, ComponentInfoMessage>();

        public BusDiscoveryClient(IBus bus)
        {
            _bus = bus;

            bus.RegisterBroadcastHandler<ComponentInfoMessage>(HandleComponentInfoMessage);
        }

        public Task<IEnumerable<ComponentInfoMessage>> DiscoverComponents(int timeoutSeconds = 10)
        {
            return DiscoverComponents(TimeSpan.FromSeconds(timeoutSeconds));
        }

        public async Task<IEnumerable<ComponentInfoMessage>> DiscoverComponents(TimeSpan timeout)
        {
            _bus.Broadcast(new DiscoveryMessage());

            await Task.Delay(timeout);

            return _components.Values;
        }

        public void ClearCache()
        {
            _components.Clear();
        }

        private void HandleComponentInfoMessage(ComponentInfoMessage componentInfo)
        {
            var key = $"{componentInfo.ModuleName}.{componentInfo.InstanceName}.{componentInfo.RunGuid}";

            _components.AddOrUpdate(key, k => componentInfo, (u, c) => componentInfo);
        }
    }
}
