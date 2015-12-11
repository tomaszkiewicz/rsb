using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace RSB.Tests
{
    public static class ServiceBusConfig
    {
        public static void RegisterJsonTypes<T>(Assembly assembly, Formatting formatting, JsonSerializerSettings jsonSerializerSettings) where T : class
        {
            foreach (var type in GetEnumerableOfType<T>(assembly))
            {
                foreach (var prop in type.GetType().GetProperties())
                {
                    prop.SetValue(type, GetDefault(prop.PropertyType));
                }
                JsonConvert.SerializeObject(type, formatting, jsonSerializerSettings);
            }
        }
        
        private static IEnumerable<object> GetEnumerableOfType<T>(Assembly assembly)
        {
            var objects = new List<object>();
            var allTypes = assembly.GetTypes();
            foreach (Type type in
                allTypes.Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)))
                )
            {
                objects.Add(Activator.CreateInstance(type));
            }
            return objects;
        }

        private static object GetDefault(Type type)
        {
            try
            {
                if(type == typeof(string))
                    return string.Empty;
                if (type.IsClass || type.IsPrimitive)
                {
                    return Activator.CreateInstance(type);
                }
            }
            catch
            {
                return null;
            }
            return null;
        }
    }
}
