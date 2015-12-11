using System.Web.Http.Dependencies;
using StructureMap;

namespace RSB.Diagnostics.HealthChecker.Web.DependencyResolution
{
    public class StructureMapDependencyResolver : StructureMapScope, IDependencyResolver
    {
        private readonly IContainer _container;

        public StructureMapDependencyResolver(IContainer container)
            : base(container)
        {
            this._container = container;
        }

        public IDependencyScope BeginScope()
        {
            var childContainer = this._container.GetNestedContainer();
            return new StructureMapScope(childContainer);
        }
    }
}