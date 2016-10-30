using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Provausio.Tower.Core;
using Provausio.Tower.Core.Extensions;

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

        [Route(""), HttpPost]
        public async Task<HttpResponseMessage> Subscribe(FormDataCollection form)
        {
            if (form == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "no parameters were provided.");

            var topic = form.Get("hub.topic");
            var callback = form.Get("hub.callback");
            var token = form.Get("hub.verify_token");
            var mode = form.Get("hub.mode");

            var secret = form.Get("hub.secret");

            /* Per section 5.1. Only use the secret if provided via https */
#if !DEBUG
            
            if (!string.IsNullOrEmpty(secret) &&
                !Request.RequestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "may not specify secret over unsecure connection");
#endif
            if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(callback) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(mode))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "invalid parameters");

            try
            {
                if (mode == "subscribe")
                {
                    Trace.WriteLine("SERVER: Received UNSUBSCRIBE request...");

                    var newSubscription = new Subscription(topic, new Uri(callback), secret);
                    var result = await _hub.Subscribe(newSubscription, token);
                    var response = result.SubscriptionSucceeded
                        ? Request.CreateResponse(HttpStatusCode.Accepted)
                        : Request.CreateResponse(HttpStatusCode.BadRequest, result.Reason);

                    Trace.WriteLine(result.SubscriptionSucceeded
                        ? "SERVER: Subscription suceeded!"
                        : "SERVER: Client subscription failed!");

                    return response;
                }

                if (mode == "unsubscribe")
                {
                    Trace.WriteLine("SERVER: Received UNSUBSCRIBE request...");

                    // TODO: implement unsubscribe
                    return Request.CreateResponse(HttpStatusCode.NotImplemented);
                }

                return Request.CreateResponse(HttpStatusCode.NotFound);

            }
            catch(Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [Route("publish"), HttpPost]
        public async Task<HttpResponseMessage> Publish(string topic)
        {
            Trace.WriteLine("SERVER: Received a publication...");

            /* Section 7 */
            Uri requestedHubLocation;
            if(!VerifyHeaders(Request.Headers, out requestedHubLocation))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "missing required headers");

            var content = Request.Content;
            if (topic == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "topic id");

            if (content == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "content");
            
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
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            Trace.WriteLine("SERVER: Queued notifications. Subscribers will be notified shortly!");
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        private static bool VerifyHeaders(HttpHeaders headers, out Uri hubLocation)
        {
            IEnumerable<string> headerValues;
            if (!headers.TryGetValues("link", out headerValues))
            {
                hubLocation = null;
                return false;
            }

            var valueList = headerValues.ToList();
            var linkHeaders = new List<LinkHeader>();
            foreach(var value in valueList)
                linkHeaders.AddRange(LinkHeader.Parse(value));

            if (HasRequiredHeaders(linkHeaders))
            {
                var hub = linkHeaders.First(h => h.Relationship.Equals("hub", StringComparison.OrdinalIgnoreCase));
                hubLocation = hub.Link;
                return true;
            }

            hubLocation = null;
            return false;
        }

        private static bool HasRequiredHeaders(IReadOnlyCollection<LinkHeader> headers)
        {
            return headers.Any(header => header.Relationship.Equals("self")) &&
                   headers.Any(header => header.Relationship.Equals("hub"));
        }
    }
}