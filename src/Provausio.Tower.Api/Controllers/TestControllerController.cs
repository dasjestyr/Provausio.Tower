using System.Diagnostics;
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
        public async Task<HttpResponseMessage> VerifySubscription(
            [FromUri(Name = "hub.verify_token")] string verifyToken,
            [FromUri(Name = "hub.challenge")] string challenge,
            [FromUri(Name = "hub.topic")] string topic,
            [FromUri(Name = "hub.mode")] string mode)
        {
            Trace.WriteLine("CLIENT: Server requested verification of subscription. Verified!");
            return Request.CreateResponse(HttpStatusCode.OK, challenge);
        }

        [Route("do"), HttpPost]
        public async Task<HttpResponseMessage> Notify()
        {
            Trace.WriteLine("CLIENT: Received push notification!");
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}