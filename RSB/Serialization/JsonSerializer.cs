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
        private static readonly DefaultContractResolver ContractResolver = new CamelCasePropertyNamesContractResolver
        {
            //IgnoreSerializableInterface = true,
            //IgnoreSerializableAttribute = true,
        };

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = ContractResolver,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        static JsonSerializer()
        {
            JsonSettings.Converters.Add(new IsoDateTimeConverter()
            {
                DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff'Z'" 
            });

            JsonSettings.Converters.Add(new StringEnumConverter()
            {
                CamelCaseText = false
            });

            JsonSettings.Converters.Add(new JsonExceptionConverter(ContractResolver));
        }

        public object Deserialize(string str, Type type)
        {
            try
            {
                return JsonConvert.DeserializeObject(str, type, JsonSettings);
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
                return (T)JsonConvert.DeserializeObject(str, typeof(T), JsonSettings);
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
                return JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(bytes), type, JsonSettings);
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
                return (T)JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(bytes), typeof(T), JsonSettings);
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
                return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, Formatting.Indented, JsonSettings));
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
