using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace RSB.Serialization
{
    public class JsonExceptionConverter : JsonConverter
    {
        private readonly DefaultContractResolver _contractResolver;

        public JsonExceptionConverter(DefaultContractResolver contractResolver)
        {
            _contractResolver = contractResolver;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
        
        public override bool CanConvert(Type type)
        {
            return typeof(Exception).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            var jObject = serializer.Deserialize<JObject>(reader);

            if (jObject == null)
                return null;

            var newSerializer = new Newtonsoft.Json.JsonSerializer()
            {
                ContractResolver = _contractResolver
            };
            
            var value = jObject.ToObject(objectType, newSerializer);
            
            SetProperty(jObject, "Message", value, "_message");
            
            return value;
        }

        private void SetProperty(JObject jObject, string propertyName, object value, string fieldName)
        {
            var field = typeof(Exception).GetTypeInfo().GetDeclaredField(fieldName);
            var jsonPropertyName = _contractResolver.GetResolvedPropertyName(propertyName);

            if (jObject[jsonPropertyName] != null)
            {
                var fieldValue = jObject[jsonPropertyName].ToObject(field.FieldType);

                field.SetValue(value, fieldValue);
            }
        }
    }
}