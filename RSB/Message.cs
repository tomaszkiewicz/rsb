using System;

namespace RSB
{
    public class Message<T>
    {
        public MessageProperties Properties { get; set; }
        public T Body { get; set; }
        public Exception Exception { get; set; }
    }
}