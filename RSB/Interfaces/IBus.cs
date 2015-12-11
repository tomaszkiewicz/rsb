using System;
using System.Threading.Tasks;
using RSB.EventArgs;

namespace RSB.Interfaces
{
    public interface IBus : IDisposable
    {
        event EventHandler<BusExceptionEventArgs> DeserializationError;
        event EventHandler<BusExceptionEventArgs> ExecutionError;
        void Shutdown();

        /// <summary>
        /// Send one-way, fire and forget message that receivers handle in work queue fashon.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="logicalAddress"></param>
        /// <param name="expirationSeconds"></param>
        void Enqueue<T>(T obj, string logicalAddress = "", int expirationSeconds = 0);

        /// <summary>
        /// Calls RPC method on other component and waits for response.
        /// </summary>
        /// <typeparam name="TRequest">Type of request message</typeparam>
        /// <typeparam name="TResponse">Type of response message</typeparam>
        /// <param name="obj">Request message</param>
        /// <param name="logicalAddress">Routing key</param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        Task<TResponse> Call<TRequest, TResponse>(TRequest obj, string logicalAddress = "", int timeoutSeconds = 60) where TResponse : new();

        /// <summary>
        /// Broadcasts message to every receiver.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="logicalAddress"></param>
        /// <param name="expirationSeconds"></param>
        void Broadcast<T>(T obj, string logicalAddress = "", int expirationSeconds = 0);

        void RegisterQueueHandler<T>(Action<T> handler, string logicalAddress = "", TaskFactory taskFactory = null) where T : new();
        void RegisterAsyncQueueHandler<T>(Func<T, Task> handler, string logicalAddress = "", TaskFactory taskFactory = null) where T : new();

        void RegisterBroadcastHandler<T>(Action<T> handler, string logicalAddress = "", TaskFactory taskFactory = null) where T : new();
        void RegisterAsyncBroadcastHandler<T>(Func<T, Task> handler, string logicalAddress = "", TaskFactory taskFactory = null) where T : new();

        void RegisterCallHandler<TRequest, TResponse>(Func<TRequest, TResponse> handler, string logicalAddress = "", TaskFactory taskFactory = null)
            where TResponse : new()
            where TRequest : new();
        void RegisterAsyncCallHandler<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler, string logicalAddress = "", TaskFactory taskFactory = null)
            where TResponse : new()
            where TRequest : new();

        void PrepareEnqueue<T>() where T : new();
        void PrepareBroadcast<T>() where T : new();
        void PrepareCall<TRequest, TResponse>()
            where TRequest : new()
            where TResponse : new();
    }
}