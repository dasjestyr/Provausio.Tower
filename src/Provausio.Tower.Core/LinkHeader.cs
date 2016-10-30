using System;
using System.Collections.Generic;
using System.Linq;

namespace Provausio.Tower.Core
{
    public class LinkHeader
    {
        /// <summary>
        /// Gets the link.
        /// </summary>
        /// <value>
        /// The link.
        /// </value>
        public Uri Link { get; private set; }

        /// <summary>
        /// Gets the relationship.
        /// </summary>
        /// <value>
        /// The relationship.
        /// </value>
        public string Relationship { get; private set; }

        /// <summary>
        /// Parses the specified header value.
        /// </summary>
        /// <param name="headerValue">The header value.</param>
        /// <returns></returns>
        /// <exception cref="FormatException">
        /// Link headers require 2 parts, a link and and a rel
        /// or
        /// Missing rel
        /// or
        /// Rel format is rel=value
        /// or
        /// Link must be an http url
        /// </exception>
        public static IEnumerable<LinkHeader> Parse(string headerValue)
        {
            var linkHeaders = new List<LinkHeader>();
            var links = headerValue.Split(',');

            foreach (var link in links)
            {
                var parts = link.Split(';');
                if (parts.Length != 2)
                    throw new FormatException("Link headers require 2 parts, a link and and a rel");

                // validate relationship
                var relPair = parts.Where(p => p.Trim().StartsWith("rel", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (relPair.Length != 1)
                    throw new FormatException("Missing rel");

                var relValue = relPair[0].Split('=');
                if (relValue.Length != 2)
                    throw new FormatException("Rel format is rel=value");

                // validate link
                var uriLink = parts.Where(p => p.Trim().Trim('<', '>', '/').StartsWith("http")).ToArray(); // should also account for https
                if (uriLink.Length != 1)
                    throw new FormatException("Link must be an http url");

                var l = new Uri(uriLink[0].Trim().Trim('<', '>', '/'));
                var r = relValue[1].Trim('\"');

                linkHeaders.Add(new LinkHeader { Link = l, Relationship = r });
            }

            return linkHeaders;
        }
    }
}
