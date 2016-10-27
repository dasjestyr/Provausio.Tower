using System;
using System.Collections.Concurrent;

namespace Provausio.Tower.Core
{
    public class InMemoryPublishQueue : IPublishQueue
    {
        private readonly IProducerConsumerCollection<Publication> _queue;

        public InMemoryPublishQueue()
            : this(new ConcurrentQueue<Publication>())
        {
        }

        public InMemoryPublishQueue(IProducerConsumerCollection<Publication> queue)
        {
            if(queue == null)
                throw new ArgumentNullException(nameof(queue));

            _queue = queue;
        }

        public void Enqueue(Publication publication)
        {
            _queue.TryAdd(publication);
        }

        public Publication Dequeue()
        {
            Publication pub;
            _queue.TryTake(out pub);
            return pub;
        }
    }
}