using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Provausio.Tower.Core
{
    public interface IPubSubHub
    {
        /// <summary>
        /// Subscribes the callback to the specified topic.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="verifyToken">The verify token.</param>
        /// <returns></returns>
        Task<SubscriptionResult> Subscribe(Subscription subscription, string verifyToken);

        /// <summary>
        /// Notifies all subscribers of an event
        /// </summary>
        /// <param name="topic">The topic identifier.</param>
        /// <param name="payload">The payload that will be sent to subscribers.</param>
        /// <returns></returns>
        void Publish(string topic, HttpContent payload);

        /// <summary>
        /// Executes the publication notifications
        /// </summary>
        /// <param name="publication">The publication.</param>
        /// <returns></returns>
        Task PublishDirect(Publication publication);
    }
}