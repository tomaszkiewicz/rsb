using System;
using System.Threading;
using System.Threading.Tasks;

namespace RSB.Extensions
{
    public static class TimeoutAfterExtension
    {
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, int timeoutSeconds = 60)
        {
            return await task.TimeoutAfter(TimeSpan.FromSeconds(timeoutSeconds));
        }

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            if (task.IsCompleted)
                return await task;

            if (timeout == TimeSpan.Zero)
                throw new TimeoutException();

            var cs = new CancellationTokenSource();

            if (task != await Task.WhenAny(task, Task.Delay(timeout, cs.Token))) 
                throw new TimeoutException();

            cs.Cancel(false);

            return await task;
        }
    }
}
