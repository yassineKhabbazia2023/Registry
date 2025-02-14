using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Moq;
using Notifications.Commons.WebApi.QueryParams;
using Org.BouncyCastle.Tsp;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Registry.Infrastructure;
using Registry.Infrastructure.Managers;
using System.Reflection;
using System.Threading;

namespace Registry.AzureFuctions.Tests.Managers;

public class NotificationsManagerTest
{

    [Fact]
    public async Task PublishAsync_CompletedTask()
    {
        // Arrange
        Mock<IEventPublisher> eventPublisherMock = new Mock<IEventPublisher>();
        Mock<IOptions<ServiceBusOptions>> serviceBusOptions = new Mock<IOptions<ServiceBusOptions>>();
        Mock<IAzureClientFactory<ServiceBusSender>> azureClientFactory = new Mock<IAzureClientFactory<ServiceBusSender>>();
        var expectedNotifEmailRequest = new EmailRequest()
        {
            Cc = new List<string>(),
            From = "from@email.test",
            TemplateName = "template",
            To = new List<string> { "to@test.fr" }
        };
        var expectedEvent = new EmailOnlySenderEvent(expectedNotifEmailRequest);

        eventPublisherMock.Setup(b => b.PublishAsync(expectedEvent, It.IsAny<string>(), It.IsAny<string>()))
        .Callback<BaseEvent<EmailRequest>, string, string>((e, t, u) =>
        {
            var notifEvent = (EmailOnlySenderEvent)e;
            notifEvent.Data.Should().BeEquivalentTo(expectedEvent.Data);
        }).Returns(Task.CompletedTask)
        .Verifiable();

        // Act
        NotificationsManager notificationsManager = new NotificationsManager(eventPublisherMock.Object, serviceBusOptions.Object, azureClientFactory.Object);
        await notificationsManager.PublishAsync(expectedEvent, topicName: "test");

        // Assert
        eventPublisherMock.Verify(p => p.PublishAsync(expectedEvent, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task PublishToQueueAsync_CompletedTask()
    {
        // Arrange
        Mock<IEventPublisher> eventPublisherMock = new Mock<IEventPublisher>();
        Mock<IOptions<ServiceBusOptions>> serviceBusOptions = new Mock<IOptions<ServiceBusOptions>>();
        Mock<IAzureClientFactory<ServiceBusSender>> azureClientFactory = new Mock<IAzureClientFactory<ServiceBusSender>>();
        var expectedNotifEmailRequest = new EmailRequest()
        {
            Cc = new List<string>(),
            From = "from@email.test",
            TemplateName = "template",
            To = new List<string> { "to@test.fr" }
        };

        var mockServiceBusSender = new Mock<ServiceBusSender>();
        mockServiceBusSender
            .Setup(y => y.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(default(object)));

        azureClientFactory.Setup(a => a.CreateClient("test"))
            .Returns(mockServiceBusSender.Object)
            .Verifiable();

        // Act
        NotificationsManager notificationsManager = new NotificationsManager(eventPublisherMock.Object, serviceBusOptions.Object, azureClientFactory.Object);
        await notificationsManager.PublishToQueueAsync(expectedNotifEmailRequest, correlationId: "test", queueName: "test");

        // Assert
        azureClientFactory.Verify(p => p.CreateClient("test").SendMessageAsync(It.IsAny<ServiceBusMessage>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task BulkPublishAsync_CompletedTask()
    {
        // Arrange
        Mock<IEventPublisher> eventPublisherMock = new Mock<IEventPublisher>();
        Mock<IOptions<ServiceBusOptions>> serviceBusOptions = new Mock<IOptions<ServiceBusOptions>>();
        Mock<IAzureClientFactory<ServiceBusSender>> azureClientFactory = new Mock<IAzureClientFactory<ServiceBusSender>>();
        var expectedNotifEmailRequest = new EmailRequest()
        {
            Cc = new List<string>(),
            From = "from@email.test",
            TemplateName = "template",
            To = new List<string> { "to@test.fr" }
        };

        var messages = new List<ServiceBusMessage>()
        {
            new ServiceBusMessage()
        };

        List<ServiceBusMessage> backingList = new();
        int batchCountThreshold = 5;

        ServiceBusMessageBatch mockBatch = ServiceBusModelFactory.ServiceBusMessageBatch(
            batchSizeBytes: 500,
            batchMessageStore: backingList,
            batchOptions: new CreateMessageBatchOptions(),
            tryAddCallback: _ => backingList.Count < batchCountThreshold);

        var serviceBusSenderMock = new Mock<ServiceBusSender>();

        serviceBusSenderMock.Setup(callTo => callTo.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(mockBatch);

        serviceBusSenderMock
        .Setup(sender => sender.SendMessagesAsync(
            It.IsAny<ServiceBusMessageBatch>(),
            It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

        azureClientFactory.Setup(callTo => callTo.CreateClient("test"))
                                  .Returns(serviceBusSenderMock.Object);

        // Act
        NotificationsManager notificationsManager = new NotificationsManager(eventPublisherMock.Object, serviceBusOptions.Object, azureClientFactory.Object);
        await notificationsManager.BulkPublishAsync(messages, topicName: "test");

        // Assert
        azureClientFactory.Verify(p => p.CreateClient("test"), Times.Once);
        serviceBusSenderMock.Verify(p => p.CreateMessageBatchAsync(It.IsAny<CancellationToken>()), Times.Once);
        serviceBusSenderMock.Verify(p => p.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
