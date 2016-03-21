using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RSB.Transports.RabbitMQ
{
    class ChannelTcsIndex
    {
        private readonly object _itemsLock = new object();
        private readonly List<TcsIndexItem> _items = new List<TcsIndexItem>();

        public void Add(string correlationId, ulong deliveryTag, TaskCompletionSource<bool> tcs)
        {
            lock (_itemsLock)
            {
                _items.Add(new TcsIndexItem()
                {
                    CorrelationId = correlationId,
                    DeliveryTag = deliveryTag,
                    TaskCompletionSource = tcs
                });
            }
        }

        public TaskCompletionSource<bool> Remove(string correlationId)
        {
            lock (_itemsLock)
            {
                var item = _items.FirstOrDefault(i => i.CorrelationId == correlationId);

                if (item == null)
                    return null;

                _items.Remove(item);

                return item.TaskCompletionSource;
            }
        }

        public TaskCompletionSource<bool> Remove(ulong deliveryTag)
        {
            lock (_itemsLock)
            {
                var item = _items.FirstOrDefault(i => i.DeliveryTag == deliveryTag);

                if (item == null)
                    return null;

                _items.Remove(item);

                return item.TaskCompletionSource;
            }
        }

        public IEnumerable<TaskCompletionSource<bool>> RemoveMultiple(ulong deliveryTag)
        {
            lock (_itemsLock)
            {
                var items = _items.Where(i => i.DeliveryTag <= deliveryTag).ToArray();

                foreach (var item in items)
                    _items.Remove(item);

                return items.Select(i => i.TaskCompletionSource);
            }
        }

        class TcsIndexItem
        {
            public string CorrelationId { get; set; }
            public ulong DeliveryTag { get; set; }
            public TaskCompletionSource<bool> TaskCompletionSource { get; set; }
        }
    }
}
