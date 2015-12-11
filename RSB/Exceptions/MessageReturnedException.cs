using System;

namespace RSB.Exceptions
{
    public class MessageReturnedException : Exception
    {
        public ushort ReplyCode { get; private set; }
        public string ReplyText { get; private set; }

        public MessageReturnedException(ushort replyCode, string replyText)
        {
            ReplyCode = replyCode;
            ReplyText = replyText;
        }
    }
}