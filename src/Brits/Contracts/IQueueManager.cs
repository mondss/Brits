using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Brits
{
    /// <summary>
    /// An interface defining what a queue manager must implement.
    /// </summary>
    public interface IQueueManager
    {
        /// <summary>
        /// Gets the queue providers that have been registered with the current <see cref="IQueueManager"/>
        /// implementation.
        /// </summary>
        IReadOnlyList<RegisteredQueueProvider> RegisteredQueueProviders { get; }
        
        /// <summary>
        /// Adds a named queue to the internal collection of queue providers.
        /// </summary>
        /// <param name="queue">The name of the queue.</param>
        /// <param name="queueProvider">The queue provider.</param>
        void AddQueue(string queue, IQueueProvider queueProvider);

        /// <summary>
        /// Deletes a received message from a queue.static On some queueing systems this is known as confirming or
        /// acknowledging the queue message.
        /// </summary>
        /// <param name="receivedMessage">The message to delete.</param>
        /// <param name="cancellationToken">The optional cancellation token to use in asynchronous operations.</param>
        /// <returns>
        /// An asynchronous task yielding a <see cref="DeleteResponse" /> object which contains information about the 
        /// delete.
        /// </returns>
        Task<DeleteResponse> Delete(ReceivedMessage receivedMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a message from a queue. On some queueing systems this is known as confirming or acknowledging
        /// the queue message.
        /// </summary>
        /// <param name="deletable">The message to delete.</param>
        /// <param name="cancellationToken">The optional cancellation token to use in asynchronous operations.</param>
        /// <returns>
        /// An asynchronous task yielding a <see cref="DeleteResponse"/> object which contains information about the
        /// delete.
        /// </returns>
        Task<DeleteResponse> Delete(Deletable deletable, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches a message onto a queue.
        /// </summary>
        /// <param name="message">The message to dispatch.</param>
        /// <param name="cancellationToken">The optional cancellation token to use in asynchronous operations.</param>
        /// <returns>An asynchronous task yielding a <see cref="DispatchResponse"/> object.</returns>
        Task<DispatchResponse> Dispatch(Message message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Receives a message or messages from a queue.
        /// </summary>
        /// <param name="receivable">An object that configures the Receive method on a queue provider.</param>
        /// <param name="cancellationToken">The optional cancellation token to use in asynchronous operations.</param>
        /// <returns>An asynchronous task yielding a <see cref="ReceiveResponse"/> object.</returns>
        Task<ReceiveResponse> Receive(Receivable receivable, CancellationToken cancellationToken = default);
    }
}
