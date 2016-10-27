namespace Provausio.Tower.Core
{
    public interface IPublishQueue
    {
        /// <summary>
        /// Adds the publication to the queue for processing.
        /// </summary>
        /// <param name="publication">The publication.</param>
        void Enqueue(Publication publication);

        /// <summary>
        /// Pops the next item in the queue
        /// </summary>
        /// <returns></returns>
        Publication Dequeue();
    }
}
