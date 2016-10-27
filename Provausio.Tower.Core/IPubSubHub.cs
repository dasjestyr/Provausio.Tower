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
        /// <param name="topic">The topic.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="verifyToken">The verify token.</param>
        /// <returns></returns>
        Task<SubscriptionResult> Subscribe(string topic, Uri callback, string verifyToken);

        /// <summary>
        /// Notifies all subscribers of an event
        /// </summary>
        /// <param name="topicId">The topic identifier.</param>
        /// <param name="payload">The payload that will be sent to subscribers.</param>
        /// <returns></returns>
        Task Publish(object topicId, HttpContent payload);
    }
}