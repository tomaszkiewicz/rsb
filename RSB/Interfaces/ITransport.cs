using System;
using System.Threading.Tasks;

namespace RSB.Interfaces
{
    public interface ITransport
    {
        bool IsConnected { get; }
        
        void Enqueue<T>(string logicalAddress, MessageProperties properties, T body);
        Task<bool> Call<T>(string logicalAddress, MessageProperties properties, T body);
        void Broadcast<T>(string logicalAddress, MessageProperties properties, T body);

        void Prepare<T>() where T : new();
        void Subscribe<T>(Action<Message<T>> dispatcher, string logicalAddress, string listenAddress, TaskFactory taskFactory = null) where T : new();

        void Shutdown();
    }
}