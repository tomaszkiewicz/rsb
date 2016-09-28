using System;

namespace RSB.EventArgs
{
    public class ReconnectFailedEventArgs : System.EventArgs
    {
        public ReconnectFailedEventArgs()
        {
            // intentionally left blank
        }

        public ReconnectFailedEventArgs(Exception exception, string message)
        {
            Exception = exception;
            Message = message;
        }

        public Exception Exception { get; set; }
        public string Message { get; set; }
    }
}
