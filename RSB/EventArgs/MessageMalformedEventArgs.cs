namespace RSB.EventArgs
{
    public class MessageMalformedEventArgs : System.EventArgs
    {
        public string Message { get; set; }

        public MessageMalformedEventArgs()
        {
            // intentionally left blank
        }

        public MessageMalformedEventArgs(string message)
        {
            Message = message;
        }
    }
}