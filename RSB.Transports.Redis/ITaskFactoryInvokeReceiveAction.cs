namespace RSB.Transports.Redis
{
    internal interface ITaskFactoryInvokeReceiveAction
    {
        void CallDispatcher(string messageBody);
    }
}