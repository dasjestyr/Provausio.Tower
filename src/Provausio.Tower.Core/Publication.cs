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
        /// Initializes a new instance of the <see cref="Publication"/> class.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="payload">The payload.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public Publication(string topic, HttpContent payload)
        {
            if(string.IsNullOrEmpty(topic))
                throw new ArgumentNullException(nameof(topic));

            if(payload == null)
                throw new ArgumentNullException(nameof(payload));

            Topic = topic;
            Payload = payload;
        }
    }
}