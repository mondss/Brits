using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moogie.Queues.Internal;

namespace Moogie.Queues.Providers.Memory
{
    /// <summary>
    /// In memory queue provider for testing purposes.
    /// </summary>
    public class MemoryProvider : IQueueProvider
    {
        private readonly ConcurrentDictionary<string, QueuedMessage> _messages =
            new ConcurrentDictionary<string, QueuedMessage>();

        /// <inheritdoc />
        public Task<DeleteResponse> Delete(Deletable deletable)
        {
            _messages.TryRemove(deletable.ReceiptHandle, out _);
            return Task.FromResult(new DeleteResponse());
        }

        /// <inheritdoc />
        public Task<DispatchResponse> Dispatch(Message message)
        {
            var messageId = message.Id ?? Guid.NewGuid();

            _messages.TryAdd(messageId.ToString(), new QueuedMessage
            {
                Id = messageId,
                Content = message.Content,
                Expiry = message.Expiry,
                Queue = message.Queue
            });

            return Task.FromResult(new DispatchResponse {MessageId = messageId});
        }

        /// <summary>
        /// Gets whether or not the memory provider contains the message.
        /// </summary>
        /// <param name="id">The id of the message.</param>
        public bool HasMessage(Guid id) => _messages.ContainsKey(id.ToString());

        /// <summary>
        /// Gets whether there are any messages in the memory provider.
        /// </summary>
        public bool HasMessages => _messages.Any();

        /// <inheritdoc />
        public async Task<ReceiveResponse> Receive(Receivable receivable)
            => receivable.SecondsToWait != null ? await LongPoll(receivable) : GetMessages(receivable);

        private async Task<ReceiveResponse> LongPoll(Receivable receivable)
        {
            var messagesReceived = new List<ReceivedMessage>();
            // ReSharper disable once PossibleInvalidOperationException
            var timeToWaitUntil = DateTime.Now.AddSeconds(receivable.SecondsToWait.Value);

            bool ShouldContinuePolling() => messagesReceived.Count < receivable.MessagesToReceive &&
                DateTime.Now <= timeToWaitUntil;

            while (ShouldContinuePolling())
            {
                messagesReceived.AddRange(GetMessages(receivable).Messages);
                if (ShouldContinuePolling())
                    await Task.Delay(250);
            }

            return new ReceiveResponse {Messages = messagesReceived};
        }

        private ReceiveResponse GetMessages(Receivable receivable)
        {
            var messages = _messages
                .Where(x => x.Value.Expiry == null || x.Value.Expiry > DateTime.Now)
                .Take(receivable.MessagesToReceive)
                .Select(x => new ReceivedMessage
                {
                    Id = x.Value.Id,
                    Content = x.Value.Content,
                    ReceiptHandle = x.Value.Id.ToString(),
                    Queue = x.Value.Queue
                });

            return new ReceiveResponse {Messages = messages};
        }
    }
}