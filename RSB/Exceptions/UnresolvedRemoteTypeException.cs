namespace RSB.Exceptions
{
    public class UnresolvedRemoteTypeException : RemoteException
    {
        public UnresolvedRemoteTypeException(string message) : base(message)
        {
        }

        public string TypeName { get; set; }
    }
}