using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Xunit;

namespace Brits.Tests.Providers.AmazonSQS
{
    public class DispatchTests : BaseSQSProviderTests
    {
        [Fact]
        public async Task It_Dispatches_A_Message()
        {
            // Arrange.
            var expiry = DateTime.Now.AddDays(1);
            var message = Message.WithContent("abc").WhichExpiresAt(expiry);

            // Act.
            var response = await QueueManager.Dispatch(message);
 
            // Assert.
            Assert.NotEqual(Guid.Empty, response.MessageId);

            var receivedMessage = await SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = QueueUrl,
                WaitTimeSeconds = 1,
                MaxNumberOfMessages = 1
            });
            Assert.NotEmpty(receivedMessage.Messages);

            var deserialisedMessage = JsonSerializer.Deserialize<ReceivedMessage>(receivedMessage.Messages.First().Body);
            Assert.Equal(response.MessageId, deserialisedMessage.Id);
            Assert.Equal("default", deserialisedMessage.Queue);
            Assert.Equal("abc", deserialisedMessage.Content);
            Assert.Equal(expiry, deserialisedMessage.Expiry);
        }
    }
}
