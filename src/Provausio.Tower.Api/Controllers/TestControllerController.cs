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
        [Route("do1"), HttpGet]
        public async Task<HttpResponseMessage> VerifySubscription1(
            [FromUri(Name = "hub.verify_token")] string verifyToken,
            [FromUri(Name = "hub.challenge")] string challenge,
            [FromUri(Name = "hub.topic")] string topic,
            [FromUri(Name = "hub.mode")] string mode)
        {
            Trace.WriteLine("CLIENT: Server requested verification of subscription with callback 1. Verified!");
            return Request.CreateResponse(HttpStatusCode.OK, challenge);
        }

        [Route("do2"), HttpGet]
        public async Task<HttpResponseMessage> VerifySubscription2(
            [FromUri(Name = "hub.verify_token")] string verifyToken,
            [FromUri(Name = "hub.challenge")] string challenge,
            [FromUri(Name = "hub.topic")] string topic,
            [FromUri(Name = "hub.mode")] string mode)
        {
            Trace.WriteLine("CLIENT: Server requested verification of subscription with callback 2. Verified!");
            return Request.CreateResponse(HttpStatusCode.OK, challenge);
        }

        [Route("do1"), HttpPost]
        public async Task<HttpResponseMessage> Notify1()
        {
            Trace.WriteLine("CLIENT: Received push notification on callback 1!");
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        [Route("do2"), HttpPost]
        public async Task<HttpResponseMessage> Notify2()
        {
            Trace.WriteLine("CLIENT: Received push notification on callback 2!");
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}