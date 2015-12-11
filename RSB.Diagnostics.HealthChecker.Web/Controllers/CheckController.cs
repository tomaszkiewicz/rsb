using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace RSB.Diagnostics.HealthChecker.Web.Controllers
{
    [RoutePrefix("components")]
    public class CheckController : ApiController
    {
        private readonly HealthCheckerService _healthCheckerService;

        public CheckController(HealthCheckerService healthCheckerService)
        {
            _healthCheckerService = healthCheckerService;
        }

        [HttpGet]
        [Route]
        public IEnumerable<object> CheckAllComponentsHealth()
        {
            return _healthCheckerService.GetComponentsHealth().Select(c => new
            {
                Name = c.Key,
                Health = c.Value.Health,
                Subsystems = c.Value.Subsystems
            });
        }

        [HttpGet]
        [Route("{component}")]
        public HttpResponseMessage CheckHealthForSingleComponent(string component)
        {
            var componentsHealth = _healthCheckerService.GetComponentsHealth();

            // TODO check also with first letter upper - json serialization forces camel case on /components

            if (!componentsHealth.ContainsKey(component))
                return Request.CreateResponse(HttpStatusCode.NotFound);

            return Request.CreateResponse(MapToHttpStatusCode(componentsHealth[component].Health), componentsHealth[component]);
        }

        private HttpStatusCode MapToHttpStatusCode(HealthState state)
        {
            switch (state)
            {
                case HealthState.Healthy:
                    return HttpStatusCode.OK;

                default:
                    return HttpStatusCode.ServiceUnavailable;
            }
        }
    }
}