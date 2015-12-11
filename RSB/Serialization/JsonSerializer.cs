using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RSB.Exceptions;

namespace RSB.Serialization
{
    public class JsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
            {
                IgnoreSerializableInterface = true,
                IgnoreSerializableAttribute = true,
            },
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public object Deserialize(string str, Type type)
        {
            try
            {
                return JsonConvert.DeserializeObject(str, type, _jsonSettings);
            }
            catch (Exception ex)
            {
                throw new SerializationException(ex);
            }
        }

        public T Deserialize<T>(string str) where T : new()
        {
            try
            {
                return (T)JsonConvert.DeserializeObject(str, typeof(T), _jsonSettings);
            }
            catch (Exception ex)
            {
                throw new SerializationException(ex);
            }
        }
        public object Deserialize(byte[] bytes, Type type)
        {
            try
            {
                return JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(bytes), type, _jsonSettings);
            }
            catch (Exception ex)
            {
                throw new SerializationException(ex);
            }
        }

        public T Deserialize<T>(byte[] bytes) where T : new()
        {
            try
            {
                return (T)JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(bytes), typeof(T), _jsonSettings);
            }
            catch (Exception ex)
            {
                throw new SerializationException(ex);
            }
        }

        public void PrepareSerialization<T>() where T : new()
        {
            try
            {
                Deserialize<T>(Serialize(new T()));
            }
            catch (Exception ex)
            {
                throw new SerializationException(ex);
            }
        }

        public byte[] Serialize<T>(T obj)
        {
            try
            {
                return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, Formatting.Indented, _jsonSettings));
            }
            catch (Exception ex)
            {
                throw new SerializationException(ex);
            }
        }

        public string ContentType { get { return "application/json"; } }
        public string Encoding { get { return "utf8"; } }
    }
}
