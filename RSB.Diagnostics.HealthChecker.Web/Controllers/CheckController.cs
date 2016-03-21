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
        public IEnumerable<ComponentHealth> CheckAllComponentsHealth()
        {
            return _healthCheckerService.GetComponentsHealth();
        }

        [HttpGet]
        [Route("{component}")]
        public HttpResponseMessage CheckHealthForSingleComponent(string component)
        {
            var componentHealth = _healthCheckerService.GetComponentsHealth().FirstOrDefault(c => c.ComponentName == component);

            // TODO check also with first letter upper - json serialization forces camel case on /components

            if (componentHealth == null)
                return Request.CreateResponse(HttpStatusCode.NotFound);

            return Request.CreateResponse(MapToHttpStatusCode(componentHealth.Health), componentHealth);
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