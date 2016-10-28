using System;

namespace Provausio.Tower.Core
{
    public class SubscriberEvent
    {
        /// <summary>
        /// Gets the event identifier.
        /// </summary>
        /// <value>
        /// The event identifier.
        /// </value>
        public string Topic { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriberEvent"/> class.
        /// </summary>
        /// <param name="topic">The event identifier.</param>
        /// <param name="description">The description.</param>
        /// <exception cref="ArgumentException">
        /// Default GUID value.
        /// or
        /// Event description cannot be null or empty.
        /// </exception>
        public SubscriberEvent(string topic, string description)
        {
            if(string.IsNullOrEmpty(topic))
                throw new ArgumentNullException(nameof(topic));

            if(string.IsNullOrEmpty(description))
                throw new ArgumentException("Event description cannot be null or empty.", nameof(description));

            Topic = topic;
            Description = description;
        }

    }
}