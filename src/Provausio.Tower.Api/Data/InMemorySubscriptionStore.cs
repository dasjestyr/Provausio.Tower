using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Provausio.Tower.Core;
using Provausio.Tower.Core.Extensions;

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

                /* 5.1 override existing pubs - no need to override; a new callback is a different sub */
                // TODO: consider only allowing a subscriber to make one subscription to a topic?
                if (original.Any(sub => sub.Callback == subscription.Callback))
                    return Task.CompletedTask;

                var newValues = original.Clone();
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
            if (!_store.ContainsKey(subscription.Topic))
                return Task.CompletedTask;

            var original = _store[subscription.Topic];
            var newValues = original.Clone();
            newValues.RemoveAll(sub => sub.Callback == subscription.Callback);

            _store.TryUpdate(subscription.Topic, newValues, original);

            return Task.CompletedTask;
        }

        public Task CreateEvent(SubscriberEvent subscriptionEvent)
        {
            throw new NotImplementedException();
        }

        private IList<T> CloneList<T>(IEnumerable<T> source) 
        {
            var sourceArray = source.ToArray();
            var newArray = new T[sourceArray.Length];
            sourceArray.CopyTo(newArray, 0);

            return new List<T>(newArray);
        }
    }
}
