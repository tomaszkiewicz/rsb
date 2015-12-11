using System;

namespace RSB.Serialization
{
    public interface ISerializer
    {
        object Deserialize(string str, Type type);
        T Deserialize<T>(string str) where T : new();
        object Deserialize(byte[] bytes, Type type);
        T Deserialize<T>(byte[] bytes) where T : new();
        void PrepareSerialization<T>() where T : new();
        byte[] Serialize<T>(T obj);
        string ContentType { get; }
        string Encoding { get; }
    }
}