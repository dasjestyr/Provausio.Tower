using System;
using System.Net.Http;

namespace Provausio.Tower.Core
{
    public class Publication
    {
        /// <summary>
        /// Gets the topic.
        /// </summary>
        /// <value>
        /// The topic.
        /// </value>
        public string Topic { get; }

        /// <summary>
        /// Gets the payload.
        /// </summary>
        /// <value>
        /// The payload.
        /// </value>
        public HttpContent Payload { get; }

        /// <summary>
        /// Gets the hub location.
        /// </summary>
        /// <value>
        /// The hub location.
        /// </value>
        public Uri HubLocation { get; }


        /// <summary>
        /// Initializes a new instance of the <see cref="Publication" /> class.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="payload">The payload.</param>
        /// <param name="hubLocation">The hub location.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        /// <exception cref="ArgumentException">Must include self and hub headers at minimum</exception>
        public Publication(string topic, HttpContent payload, Uri hubLocation)
        {
            if(string.IsNullOrEmpty(topic))
                throw new ArgumentNullException(nameof(topic));

            if(payload == null)
                throw new ArgumentNullException(nameof(payload));

            if(hubLocation == null)
                throw new ArgumentNullException(nameof(hubLocation));

            Topic = topic;
            Payload = payload;
            HubLocation = hubLocation;
        }
    }
}