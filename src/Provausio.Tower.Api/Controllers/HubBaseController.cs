using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Results;
using Provausio.Tower.Core;

namespace Provausio.Tower.Api.Controllers
{
    public class HubBaseController : ApiController
    {
        protected IHttpActionResult ErrorResponse(HttpStatusCode code, string message)
        {
            var error = new ApplicationError(message);
            var msg = Request.CreateResponse(code, error, new JsonMediaTypeFormatter());
            return ResponseMessage(msg);
        }

        protected static bool VerifyHeaders(HttpHeaders headers, out Uri hubLocation)
        {
            IEnumerable<string> headerValues;
            if (!headers.TryGetValues("link", out headerValues))
            {
                hubLocation = null;
                return false;
            }

            var valueList = headerValues.ToList();
            var linkHeaders = new List<LinkHeader>();

            foreach (var value in valueList)
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

        protected static bool HasRequiredHeaders(IReadOnlyCollection<LinkHeader> headers)
        {
            return headers.Any(header => header.Relationship.Equals("self")) &&
                   headers.Any(header => header.Relationship.Equals("hub"));
        }

        protected bool SubscriptionParametersAreValid(FormDataCollection data)
        {
            var topic = data.Get("hub.topic");
            var callback = data.Get("hub.callback");
            var token = data.Get("hub.verify_token");
            var mode = data.Get("hub.mode");
            
#if !DEBUG
            /* Per section 5.1. Only use the secret if provided via https */
            var secret = data.Get("hub.secret");
            if (!string.IsNullOrEmpty(secret) && !HttpContext.Current.Request.IsSecureConnection)
                throw new HttpException(403, "You may not specify a secret over a non secure connection");
#endif
            return
                !string.IsNullOrEmpty(topic) &&
                !string.IsNullOrEmpty(callback) &&
                !string.IsNullOrEmpty(token) &&
                !string.IsNullOrEmpty(mode);
        }

        protected IHttpActionResult Accepted()
        {
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.Accepted));
        }
    }
}