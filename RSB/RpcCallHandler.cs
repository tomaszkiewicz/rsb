using System;
using System.Threading.Tasks;

namespace RSB
{
    internal class RpcCallHandler
    {
        public TaskCompletionSource<object> TaskCompletion { get; set; }
        public Type ResponseType { get; set; }
    }
}