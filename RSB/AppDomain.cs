using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace RSB
{
    /// <summary>
    /// Polyfill from http://www.michael-whelan.net/replacing-appdomain-in-dotnet-core/
    /// </summary>
    public class AppDomain
    {
        public static AppDomain CurrentDomain { get; }

        static AppDomain()
        {
            CurrentDomain = new AppDomain();
        }

        public Assembly[] GetAssemblies()
        {
            var assemblies = new List<Assembly>();
            var dependencies = DependencyContext.Default.RuntimeLibraries;

            foreach (var library in dependencies)
            {
                if (IsCandidateCompilationLibrary(library))
                {
                    var assembly = Assembly.Load(new AssemblyName(library.Name));
                    assemblies.Add(assembly);
                }
            }
            return assemblies.ToArray();
        }

        private static bool IsCandidateCompilationLibrary(RuntimeLibrary compilationLibrary)
        {
            return compilationLibrary.Name == ("Specify")
                   || compilationLibrary.Dependencies.Any(d => d.Name.StartsWith("Specify"));
        }
    }
}