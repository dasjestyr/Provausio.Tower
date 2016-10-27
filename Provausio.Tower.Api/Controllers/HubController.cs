using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using Provausio.Tower.Core;

namespace Provausio.Tower.Api.Controllers
{
    [RoutePrefix("hub")]
    public class HubController : ApiController
    {
        private readonly IPubSubHub _hub;

        public HubController(IPubSubHub hub)
        {
            _hub = hub;
        }

        [Route("subscribe"), HttpPost]
        public async Task<HttpResponseMessage> Subscribe(FormDataCollection data)
        {
            var callback = data["hub.callback"];
            var mode = data["hub.mode"];
            var topic = data["hub.topic"];
            var token = data["hub.verify_token"];

            if (string.IsNullOrEmpty(callback) || string.IsNullOrEmpty(mode) || string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(token) || !mode.Equals("subscribe"))
                return Request.CreateResponse(HttpStatusCode.BadRequest);

            try
            {
                var result = await _hub.Subscribe(topic, new Uri(callback), token);
                var response = result.SubscriptionSucceeded
                    ? Request.CreateResponse(HttpStatusCode.Accepted)
                    : Request.CreateResponse(HttpStatusCode.BadRequest, result.Reason);

                return response;
            }
            catch(Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
            

            return null;
        }
    }
}