using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Moogie.Queues.Internal;

namespace Moogie.Queues
{
    /// <summary>
    /// Moogie.Queues provider for Azure's Queue Storage.
    /// </summary>
    public class AzureQueueStorageProvider : BaseProvider
    {
        private const string MESSAGE_ID = "MessageId";
        private const string POP_RECEIPT = "PopReceipt";

        private readonly AzureQueueStorageOptions _options;
        private readonly QueueClient _azureQueueClient;

        /// <inheritdoc />
        public override string ProviderName { get; } = nameof(AzureQueueStorageProvider);

        /// <summary>
        /// Initialises a new instance of the <see cref="AzureQueueStorageProvider" /> class.
        /// </summary>
        /// <param name="options">The options used to configure the provider.</param>
        public AzureQueueStorageProvider(AzureQueueStorageOptions options)
        {
            _options = options;
            _azureQueueClient = new QueueClient(_options.ConnectionString, _options.QueueName);
        }

        /// <inheritdoc />
        public override async Task<DeleteResponse> Delete(Deletable deletable,
            CancellationToken cancellationToken = default)
        {
            var messageId = deletable.DeletionAttributes[MESSAGE_ID];
            var popReceipt = deletable.DeletionAttributes[POP_RECEIPT];

            var response = await _azureQueueClient
                .DeleteMessageAsync(messageId, popReceipt, cancellationToken)
                .ConfigureAwait(false);
            return new DeleteResponse {Success = response.Status == 204};
        }

        /// <inheritdoc />
        public override async Task<DispatchResponse> Dispatch(Message message,
            CancellationToken cancellationToken = default)
        {
            var serialisedMessage = await message.Serialise(cancellationToken);
            
            var timeToLive = message.Expiry != null ? message.Expiry - DateTime.Now : null;
            var response = await _azureQueueClient
                .SendMessageAsync(serialisedMessage, cancellationToken)
                .ConfigureAwait(false);

            return new DispatchResponse {MessageId = message.Id};
        }

        /// <inheritdoc />
        public override async Task<ReceiveResponse> Receive(Receivable receivable, CancellationToken cancellationToken = default)
        {
            if (receivable.SecondsToWait != null && !_options.IgnoreLongPollingException)
                throw new FeatureNotYetSupportedException("AzureQueueStorageProvider: Long polling");

            var messages = await _azureQueueClient
                .ReceiveMessagesAsync(receivable.MessagesToReceive, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var messagesToReturn = new List<ReceivedMessage>();
            foreach (var message in messages.Value)
            {
                var deletable = Deletable.OffOfQueue(receivable.Queue)
                    .WithDeletionAttribute(MESSAGE_ID, message.MessageId)
                    .WithDeletionAttribute(POP_RECEIPT, message.PopReceipt);

                var handledMessage = await DeserialiseAndHandle(message.MessageText, deletable, cancellationToken)
                    .ConfigureAwait(false);
                if (handledMessage != null)
                    messagesToReturn.Add(handledMessage);
            }
            
            return new ReceiveResponse { Messages = messagesToReturn };
        }
    }
}