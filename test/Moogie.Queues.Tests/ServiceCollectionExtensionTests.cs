using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Moogie.Queues.Tests
{
    public class ServiceCollectionExtensionTests
    {
        [Fact]
        public async Task It_Adds_The_Queue_Manager_To_The_Service_Collection()
        {
            // Arrange.
            var serviceCollection = new ServiceCollection();

            var providerOne = new ProviderOne();
            var providerTwo = new ProviderTwo();
            serviceCollection.AddMoogieQueues(new QueueRegistration {Name = "one", QueueProvider = providerOne},
                new QueueRegistration {Name = "two", QueueProvider = providerTwo});

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var queueManager = serviceProvider.GetService<IQueueManager>();

            // Act.
            await queueManager.Dispatch(Message.OnQueue("two").WithContent("hello, world"));

            // Assert.
            Assert.Empty(providerOne.DispatchedMessages);
            Assert.Single(providerTwo.DispatchedMessages, x => x.Content == "hello, world");
        }

        private class ProviderOne : FakeProvider
        {
        }

        private class ProviderTwo : FakeProvider
        {
        }
    }
}