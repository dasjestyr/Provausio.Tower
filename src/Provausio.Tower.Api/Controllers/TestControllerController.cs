using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Provausio.Tower.Api.Controllers
{
    [RoutePrefix("test")]
    public class TestControllerController : ApiController
    {
        [Route("do"), HttpGet]
        public async Task<HttpResponseMessage> Do(
            [FromUri(Name = "hub.verify_token")] string verifyToken,
            [FromUri(Name = "hub.challenge")] string challenge,
            [FromUri(Name = "hub.topic")] string topic,
            [FromUri(Name = "hub.mode")] string mode)
        {
            return Request.CreateResponse(HttpStatusCode.OK, challenge);
        }

    }
}