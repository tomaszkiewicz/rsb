using System.Web.Http;

namespace RSB.Diagnostics.HealthChecker.Web.Controllers
{
    public class HomeController : ApiController
    {
        [HttpGet]
        public string Index()
        {
            return "OK";
        }
    }
}