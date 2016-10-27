using System;
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
    }
}