using System;

namespace Provausio.Tower.Core
{
    public class Subscription
    {
        /// <summary>
        /// Gets the event identifier.
        /// </summary>
        /// <value>
        /// The event identifier.
        /// </value>
        public object TopicId { get; }

        /// <summary>
        /// Gets the subscriptions.
        /// </summary>
        /// <value>
        /// The subscriptions.
        /// </value>
        public Uri Callback { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription" /> class.
        /// </summary>
        /// <param name="topicId">The event identifier.</param>
        /// <param name="callback">The callback.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Subscription(object topicId, Uri callback)
        {
            if(topicId == null)
                throw new ArgumentNullException(nameof(topicId));

            TopicId = topicId;
            Callback = callback;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription" /> class.
        /// </summary>
        /// <param name="subEvent">The sub event.</param>
        /// <param name="callback">The callback.</param>
        public Subscription(SubscriberEvent subEvent, Uri callback)
            : this(subEvent.TopicId, callback)
        {
        }
    }
}