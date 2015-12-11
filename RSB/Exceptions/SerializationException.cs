using System;

namespace RSB.Exceptions
{
    public class SerializationException : Exception
    {
        public SerializationException()
        {
            // do nothing
        }

        public SerializationException(Exception ex) : base(ex.Message, ex)
        {
            // do nothing
        }
    }
}