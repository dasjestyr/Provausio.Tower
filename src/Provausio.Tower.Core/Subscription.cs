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
        public object Topic { get; }

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
        /// <param name="topic">The event identifier.</param>
        /// <param name="callback">The callback.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Subscription(string topic, Uri callback)
        {
            if(string.IsNullOrEmpty(topic))
                throw new ArgumentNullException(nameof(topic));

            Topic = topic;
            Callback = callback;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription" /> class.
        /// </summary>
        /// <param name="subEvent">The sub event.</param>
        /// <param name="callback">The callback.</param>
        public Subscription(SubscriberEvent subEvent, Uri callback)
            : this(subEvent.Topic, callback)
        {
        }
    }
}