using System;

namespace RSB.EventArgs
{
    public class BusExceptionEventArgs : System.EventArgs
    {
        public Exception Exception { get; set; }
        public object Message { get; set; }
        public Type MessageType { get; set; }
    }
}