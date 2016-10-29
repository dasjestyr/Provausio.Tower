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
        private readonly ConcurrentDictionary<string, List<string>> _store;

        public InMemorySubscriptionStore()
        {
            _store = new ConcurrentDictionary<string, List<string>>();
        }

        public Task<IEnumerable<Subscription>> GetSubscriptions(string topic)
        {
            if (!_store.ContainsKey(topic))
                return Task.FromResult(new List<Subscription>().AsEnumerable());

            var callbacks = _store[topic].Select(cb => new Subscription(topic, new Uri(cb)));
            return Task.FromResult(callbacks);
        }

        public Task Subscribe(Subscription subscription)
        {
            if (_store.ContainsKey(subscription.Topic))
            {
                var original = _store[subscription.Topic];

                if (original.Contains(subscription.Callback.ToString()))
                    return Task.CompletedTask;

                var newValues = new string[original.Count + 1];

                original.CopyTo(newValues);
                newValues[newValues.Length - 1] = subscription.Callback.ToString();

                _store.TryUpdate(subscription.Topic, newValues.ToList(), original);
            }
            else
            {
                var newList = new List<string> { subscription.Callback.ToString() };
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
