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

        [Route("subscribe"), HttpPost]
        public async Task<HttpResponseMessage> Subscribe(FormDataCollection form)
        {
            Trace.WriteLine("SERVER: Received subscription request...");
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
            if(!VerifyHeaders(Request.Headers))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "missing required headers");

            var content = Request.Content;
            if (topic == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "topic id");

            if (content == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "content");

            /* Section 7 */
            // preserve the content that was published (don't modify or transform)
            
            var requestClone = await Request.Clone();
            _hub.Publish(topic, requestClone.Content);

            Trace.WriteLine("SERVER: Queued notifications. Subscribers will be notified shortly!");
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        private static bool VerifyHeaders(HttpHeaders headers)
        {
            IEnumerable<string> headerValues;
            if (!headers.TryGetValues("link", out headerValues))
                return false;

            var valueList = headerValues.ToList();
            var linkHeaders = new List<LinkHeader>();
            foreach(var value in valueList)
                linkHeaders.AddRange(LinkHeader.Parse(value));

            return linkHeaders.Any(header => header.Relationship.Equals("self")) &&
                   linkHeaders.Any(header => header.Relationship.Equals("hub"));
        }
    }

    public class LinkHeader
    {
        public Uri Link { get; private set; }

        public string Relationship { get; private set; }

        public static IEnumerable<LinkHeader> Parse(string headerValue)
        {
            var linkHeaders = new List<LinkHeader>();
            var links = headerValue.Split(',');

            foreach (var link in links)
            {
                var parts = link.Split(';');
                if(parts.Length != 2)
                    throw new FormatException("Link headers require 2 parts, a link and and a rel");

                // validate relationship
                var relPair = parts.Where(p => p.Trim().StartsWith("rel", StringComparison.OrdinalIgnoreCase)).ToArray();
                if(relPair.Length != 1)
                    throw new FormatException("Missing rel");

                var relValue = relPair[0].Split('=');
                if(relValue.Length != 2)
                    throw new FormatException("Rel format is rel=value");

                // validate link
                var uriLink = parts.Where(p => p.Trim().Trim('<', '>', '/').StartsWith("http")).ToArray(); // should also account for https
                if(uriLink.Length != 1)
                    throw new FormatException("Link must be an http url");

                var l = new Uri(uriLink[0].Trim().Trim('<', '>', '/'));
                var r = relValue[1].Trim('\"');

                linkHeaders.Add(new LinkHeader { Link = l, Relationship = r });
            }

            return linkHeaders;
        }
    }
}