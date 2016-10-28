using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Provausio.Tower.Core;

namespace Provausio.Tower.Api.Data
{
    public class SubscriptionStore : ISubscriptionStore
    {
        public Task<IEnumerable<Subscription>> GetSubscriptions(string topic)
        {
            throw new NotImplementedException();
        }

        public Task Subscribe(Subscription subscription)
        {
            return Task.CompletedTask;
        }

        public Task Unsubscribe(Subscription subscription)
        {
            throw new NotImplementedException();
        }

        public Task CreateEvent(SubscriberEvent subscriptionEvent)
        {
            throw new NotImplementedException();
        }
    }
}
