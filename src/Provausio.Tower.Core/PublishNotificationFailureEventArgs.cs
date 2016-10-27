using System;

namespace Provausio.Tower.Core
{
    public class PublishNotificationFailureEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the failed subscription.
        /// </summary>
        /// <value>
        /// The failed subscription.
        /// </value>
        public Subscription FailedSubscription { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishNotificationFailureEventArgs"/> class.
        /// </summary>
        /// <param name="failedSubscription">The failed subscription.</param>
        /// <param name="message">The message.</param>
        public PublishNotificationFailureEventArgs(Subscription failedSubscription, string message)
        {
            if(failedSubscription == null)
                throw new ArgumentNullException(nameof(failedSubscription));

            if(string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            FailedSubscription = failedSubscription;
            Message = message;
        }
    }
}