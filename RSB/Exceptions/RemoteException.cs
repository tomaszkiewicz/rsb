using System;

namespace RSB.Exceptions
{
    public class RemoteException : Exception
    {
        public RemoteException(string message)
            : base(message)
        {

        }
    }
}
