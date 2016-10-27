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
        public object TopicId { get; }

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
        /// <param name="topicId">The event identifier.</param>
        /// <param name="description">The description.</param>
        /// <exception cref="ArgumentException">
        /// Default GUID value.
        /// or
        /// Event description cannot be null or empty.
        /// </exception>
        public SubscriberEvent(object topicId, string description)
        {
            if(topicId == null)
                throw new ArgumentNullException(nameof(topicId));

            if(string.IsNullOrEmpty(description))
                throw new ArgumentException("Event description cannot be null or empty.", nameof(description));

            TopicId = topicId;
            Description = description;
        }

    }
}