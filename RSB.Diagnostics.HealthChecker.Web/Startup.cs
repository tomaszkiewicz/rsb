using System.Web.Http;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Owin;
using RSB.Diagnostics.HealthChecker.Web;
using RSB.Diagnostics.HealthChecker.Web.DependencyResolution;
using StructureMap;

[assembly: OwinStartup(typeof(Startup))]
namespace RSB.Diagnostics.HealthChecker.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var container = new Container(x => x.AddRegistry<HealthCheckerRegistry>());

            app.UseErrorPage();

            ConfigureWebApi(app, container);
        }

        private static void ConfigureWebApi(IAppBuilder app, Container container)
        {
            var config = new HttpConfiguration();

            config.Formatters.JsonFormatter.SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            };

            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());
            
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "Home",
                routeTemplate: "",
                defaults: new { controller = "Home" }
                );

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { controller = "Home", id = RouteParameter.Optional }
                );

            config.DependencyResolver = new StructureMapDependencyResolver(container);

            app.UseWebApi(config);
        }
    }
}