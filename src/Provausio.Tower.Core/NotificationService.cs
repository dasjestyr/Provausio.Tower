using System;
using System.Threading;
using System.Threading.Tasks;

namespace Provausio.Tower.Core
{
    internal class NotificationService : IPublishQueue
    {
        private readonly Hub _hub;
        private readonly IPublishQueue _queue;
        private readonly int _emptyQueueDelay;
        private readonly SemaphoreSlim _processorSemaphore;

        private bool _isRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class.
        /// </summary>
        /// <param name="hub">The hub.</param>
        /// <param name="queue">The queue.</param>
        /// <param name="notificationThreshold">The notification threshold.</param>
        /// <param name="emptyQueueDelay">The empty queue delay.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public NotificationService(
            Hub hub, 
            IPublishQueue queue, 
            int notificationThreshold = 10, 
            int emptyQueueDelay = 3000)
        {
            if(hub == null)
                throw new ArgumentNullException(nameof(hub));

            if(queue == null)
                throw new ArgumentNullException(nameof(queue));

            _hub = hub;
            _queue = queue;
            _emptyQueueDelay = emptyQueueDelay;
            _processorSemaphore = new SemaphoreSlim(notificationThreshold, notificationThreshold);
        }

        /// <summary>
        /// Starts the background service which will continually process the queue.
        /// </summary>
        public void Start()
        {
            _isRunning = true;
            Task.Run(Run);
        }

        /// <summary>
        /// Stops processing the queue.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        private async Task Run()
        {
            while (_isRunning)
            {
                var next = _queue.Dequeue();
                if (next != null)
                {
                    await _processorSemaphore.WaitAsync();
                    ProcessNotification(next);
                }
                else
                {
                    // the queue is empty so give it a chance to gather more notifications
                    await Task.Delay(_emptyQueueDelay);
                }
            }
        }

        private async void ProcessNotification(Publication publication)
        {
            await _hub.Publish(publication);
            _processorSemaphore.Release();
        }

        public void Enqueue(Publication publication)
        {
            _queue.Enqueue(publication);
        }

        public Publication Dequeue()
        {
            return _queue.Dequeue();
        }
    }
}