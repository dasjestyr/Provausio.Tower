using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Provausio.Tower.Core;

namespace Provausio.Tower.Api.Data
{
    

    public class InMemorySubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<string, List<Subscription>> _store;

        public InMemorySubscriptionStore()
        {
            _store = new ConcurrentDictionary<string, List<Subscription>>();
        }

        public Task<IEnumerable<Subscription>> GetSubscriptions(string topic)
        {
            if (!_store.ContainsKey(topic))
                return Task.FromResult(new List<Subscription>().AsEnumerable());

            var callbacks = _store[topic].AsEnumerable();
            return Task.FromResult(callbacks);
        }

        public Task Subscribe(Subscription subscription)
        {
            if (_store.ContainsKey(subscription.Topic))
            {
                var original = _store[subscription.Topic];

                if (original.Any(sub => sub.Callback == subscription.Callback))
                    return Task.CompletedTask;

                var newValues = original;
                newValues.Add(subscription);
                
                _store.TryUpdate(subscription.Topic, newValues, original);
            }
            else
            {
                var newList = new List<Subscription> { subscription };
                _store.TryAdd(subscription.Topic, newList);
            }

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
