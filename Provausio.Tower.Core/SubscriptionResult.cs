namespace Provausio.Tower.Core
{
    public class SubscriptionResult
    {
        /// <summary>
        /// Gets a value indicating whether [subscription succeeded].
        /// </summary>
        /// <value>
        /// <c>true</c> if [subscription succeeded]; otherwise, <c>false</c>.
        /// </value>
        public bool SubscriptionSucceeded { get; internal set; }

        /// <summary>
        /// Gets the reason.
        /// </summary>
        /// <value>
        /// The reason.
        /// </value>
        public string Reason { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionResult"/> class.
        /// </summary>
        /// <param name="subscriptionSucceeded">if set to <c>true</c> [subscription succeeded].</param>
        /// <param name="reason">The reason.</param>
        public SubscriptionResult(bool subscriptionSucceeded, string reason)
        {
            SubscriptionSucceeded = subscriptionSucceeded;
            Reason = reason;
        }
    }
}