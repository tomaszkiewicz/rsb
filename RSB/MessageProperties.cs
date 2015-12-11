namespace RSB
{
    public class MessageProperties
    {
        public string ContentType { get; set; }
        public string ContentEncoding { get; set; }
        public string Type { get; set; }
        public string CorrelationId { get; set; }
        public string ReplyTo { get; set; }
        public int ExpirationMilliseconds { get; set; }
        public string ExceptionType { get; set; }
    }
}
