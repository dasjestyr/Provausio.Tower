using System.Collections.Generic;
using System.Threading.Tasks;

namespace Provausio.Tower.Core
{
    public interface ISubscriptionStore
    {
        /// <summary>
        /// Gets all subscriptions for the specified event
        /// </summary>
        /// <param name="topicId">The event identifier.</param>
        /// <returns></returns>
        Task<IEnumerable<Subscription>> GetSubscriptions(object topicId);

        /// <summary>
        /// Subscribes the specified callback URLs to the specified event
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <returns></returns>
        Task Subscribe(Subscription subscription);

        /// <summary>
        /// Unsubscribes the specified callback URLs from the specified event
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <returns></returns>
        Task Unsubscribe(Subscription subscription);

        /// <summary>
        /// Creates a new subscribably event in the event store
        /// </summary>
        /// <param name="subscriptionEvent">The subscription event.</param>
        /// <returns></returns>
        Task CreateEvent(SubscriberEvent subscriptionEvent);
    }
}