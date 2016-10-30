using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using Provausio.Tower.Core;
using Provausio.Tower.Core.Extensions;

namespace Provausio.Tower.Api.Controllers
{
    [RoutePrefix("hub")]
    public class HubController : HubBaseController
    {
        private readonly IPubSubHub _hub;

        public HubController(IPubSubHub hub)
        {
            _hub = hub;
        }

        [Route(""), HttpPost]
        public async Task<IHttpActionResult> Subscribe(FormDataCollection form)
        {
            if (form == null || !SubscriptionParametersAreValid(form))
                return ErrorResponse(HttpStatusCode.BadRequest, "Invalid parameters");

            var topic = form.Get("hub.topic");
            var callback = form.Get("hub.callback");
            var token = form.Get("hub.verify_token");
            var mode = form.Get("hub.mode");

            var secret = form.Get("hub.secret");

            try
            {
                if (mode == "subscribe")
                {
                    Trace.WriteLine("SERVER: Received UNSUBSCRIBE request...");

                    var newSubscription = new Subscription(topic, new Uri(callback), secret);
                    var result = await _hub.Subscribe(newSubscription, token);
                    var response = result.SubscriptionSucceeded
                        ? Accepted()
                        : ErrorResponse(HttpStatusCode.BadRequest, result.Reason);

                    Trace.WriteLine(result.SubscriptionSucceeded
                        ? "SERVER: Subscription suceeded!"
                        : "SERVER: Client subscription failed!");

                    return response;
                }

                if (mode == "unsubscribe")
                {
                    Trace.WriteLine("SERVER: Received UNSUBSCRIBE request...");

                    // TODO: implement unsubscribe
                    return ErrorResponse(HttpStatusCode.NotImplemented, "feature not yet implemented");
                }

                return NotFound();

            }
            catch(Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("publish"), HttpPost]
        public async Task<IHttpActionResult> Publish(string topic)
        {
            Trace.WriteLine("SERVER: Received a publication...");

            /* Section 7 */
            Uri requestedHubLocation;
            if(!VerifyHeaders(Request.Headers, out requestedHubLocation))
                return ErrorResponse(HttpStatusCode.BadRequest, "missing required headers");

            var content = Request.Content;
            if (topic == null)
                return ErrorResponse(HttpStatusCode.BadRequest, "Topic");

            if (content == null)
                return ErrorResponse(HttpStatusCode.BadRequest, "Content");
            
            /* Section 7 */
            // preserve the content that was published (don't modify or transform)
            
            var requestClone = await Request.Clone();
            var publication = new Publication(topic, requestClone.Content, requestedHubLocation);

            try
            {
                _hub.Publish(publication);
            }
            catch (RequestedHubMismatchException ex)
            {
                Trace.TraceError(ex.Message);
                return ErrorResponse(HttpStatusCode.BadRequest, "Invalid hub");
            }

            Trace.WriteLine("SERVER: Queued notifications. Subscribers will be notified shortly!");
            return Accepted();
        }

       
    }
}