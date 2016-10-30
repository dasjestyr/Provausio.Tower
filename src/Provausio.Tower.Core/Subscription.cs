using System;
using System.Text;

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
        public string Topic { get; set; }

        /// <summary>
        /// Gets the subscriptions.
        /// </summary>
        /// <value>
        /// The subscriptions.
        /// </value>
        public Uri Callback { get; set; }

        /// <summary>
        /// Gets or sets the secret used to sign publish events
        /// </summary>
        /// <value>
        /// The secret.
        /// </value>
        public string Secret { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription" /> class.
        /// </summary>
        /// <param name="topic">The event identifier.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="secret">The secret supplied by the subscriber used to salt a SHA1 hash of any publications.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Subscription(string topic, Uri callback, string secret = null)
        {
            if(string.IsNullOrEmpty(topic))
                throw new ArgumentNullException(nameof(topic));

            ValidateSecret(secret);

            Topic = topic;
            Callback = callback;
            Secret = secret;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription" /> class.
        /// </summary>
        /// <param name="subEvent">The sub event.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="secret">The secret.</param>
        public Subscription(SubscriberEvent subEvent, Uri callback, string secret = null)
            : this(subEvent.Topic, callback, secret)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class.
        /// </summary>
        public Subscription()
        {
        }

        private static void ValidateSecret(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return;

            var asBytes = Encoding.UTF8.GetBytes(secret);
            if(asBytes.Length > 200)
                throw new ArgumentException("Client secret may not exceed 200 bytes");
        }
    }
}