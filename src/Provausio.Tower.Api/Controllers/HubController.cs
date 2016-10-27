using System;
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
        public async Task<HttpResponseMessage> Subscribe(FormDataCollection form)
        {
            if (form == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "no parameters were provided.");

            var topic = form.Get("hub.topic");
            var callback = form.Get("hub.callback");
            var token = form.Get("hub.verify_token");
            var mode = form.Get("hub.mode");

            if(string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(callback) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(mode))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "invalid parameters");

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
        }

        [Route("publish"), HttpPost]
        public async Task<HttpResponseMessage> Publish(object topicId)
        {
            var content = Request.Content;
            if (topicId == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "topic id");

            if (content == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "content");

            // TODO: need a queue...
            await _hub.Publish(topicId, content);

            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
    }
}