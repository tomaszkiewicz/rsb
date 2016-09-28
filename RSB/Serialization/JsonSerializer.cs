using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RSB.Exceptions;
using RSB.Interfaces;

namespace RSB.Serialization
{
    public class JsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
            {
                // IgnoreSerializableInterface = true, <- TODO not compatible with .NET Core
                // IgnoreSerializableAttribute = true, <- TODO not compatible with .NET Core
            },
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public JsonSerializer()
        {
            _jsonSettings.Converters.Add(new IsoDateTimeConverter()
            {
                DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff'Z'" 
            });

            _jsonSettings.Converters.Add(new StringEnumConverter()
            {
                CamelCaseText = false
            });
        }

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

        public string ContentType => "application/json";
        public string Encoding => "utf8";
    }
}
