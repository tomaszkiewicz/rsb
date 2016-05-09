using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RSB
{
    public class TypeResolver
    {
        readonly ConcurrentDictionary<string, Type> _registerdTypes = new ConcurrentDictionary<string, Type>();
        readonly ConcurrentDictionary<string, Type> _typesCache = new ConcurrentDictionary<string, Type>();

        public void RegisterType<T>()
        {
            _registerdTypes[typeof(T).Name] = typeof(T);
        }

        public void RegisterType<T>(string typeName)
        {
            _registerdTypes[typeName] = typeof(T);
        }

        public void RegisterType(Type type)
        {
            _registerdTypes[type.Name] = type;
        }

        public void RegisterType(string typeName, Type type)
        {
            _registerdTypes[typeName] = type;
        }

        public Type GetType(string typeName)
        {
            Type type;

            _registerdTypes.TryGetValue(typeName, out type);

            if (type != null)
                return type;

            _typesCache.TryGetValue(typeName, out type);

            if (type != null)
                return type;

            var types = SearchTypeByName(typeName).ToArray();

            if (types.Length > 1)
                throw new InvalidOperationException($"Found more than one type for {typeName}");

            if (types.Length == 0)
                throw new InvalidOperationException($"Type {typeName} not found");

            _typesCache[typeName] = types[0];

            return types[0];
        }

        private static IEnumerable<Type> SearchTypeByName(string typeName)
        {
            var type = Type.GetType(typeName);

            if (type != null)
                yield return type;

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var typeFromAssembly in a.GetTypes())
                {
                    if (!typeof(Exception).IsAssignableFrom(typeFromAssembly))
                        continue;

                    if (typeFromAssembly.Name == typeName)
                        yield return typeFromAssembly;
                }
            }
        }
    }
}