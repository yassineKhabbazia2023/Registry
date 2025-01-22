// <copyright file="NotificationsManager.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Notifications.Commons.AzureFunctions.QueryParams;
using Notifications.Commons.WebApi;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using System.Text.Json;

namespace ContactRegistry.AzureFuctions.Managers
{
    /// <summary>
    /// NotificationsManager.
    /// </summary>
    public class NotificationsManager : INotificationManager
    {
        private readonly IEventPublisher eventPublisher;
        private readonly IOptions<ServiceBusOptions> serviceBusOptions;
        private readonly IAzureClientFactory<ServiceBusSender> azureClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationsManager"/> class.
        /// </summary>
        /// <param name="eventPublisher">Init notifications manager instance.</param>
        /// <param name="serviceBusOptions">serviceBusOptions.</param>
        /// <param name="azureClientFactory">azureClientFactory.</param>
        public NotificationsManager(IEventPublisher eventPublisher, IOptions<ServiceBusOptions> serviceBusOptions, IAzureClientFactory<ServiceBusSender> azureClientFactory)
        {
            this.eventPublisher = eventPublisher;
            this.serviceBusOptions = serviceBusOptions;
            this.azureClientFactory = azureClientFactory;
        }

        /// <inheritdoc/>
        public async Task PublishAsync<TEventData>(BaseEvent<TEventData> @event, string? topicName = default!)
        {
            ArgumentNullException.ThrowIfNull(@event);
            await this.eventPublisher.PublishAsync(@event, correlationId: null, topicName ?? this.serviceBusOptions.Value.ServiceBusRegistryTopicName);
        }

        /// <inheritdoc/>
        public async Task PublishToQueueAsync<T>(T message, string? correlationId = null, string? queueName = null)
        {
            var selectedQueue = queueName ?? this.serviceBusOptions.Value.ServiceBusContactQueueName;
            var sender = this.azureClientFactory.CreateClient(selectedQueue);
            var messageBody = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                CorrelationId = correlationId,
            };
            await sender.SendMessageAsync(serviceBusMessage);
        }

        /// <inheritdoc/>
        public async Task BulkPublishAsync(List<ServiceBusMessage> messages, string? topicName = default!)
        {
            ServiceBusMessageBatch? messageBatch = null;

            try
            {
                var selectedTopic = topicName ?? this.serviceBusOptions.Value.ServiceBusRegistryTopicName;
                var sender = this.azureClientFactory.CreateClient(selectedTopic);
                messageBatch = await sender.CreateMessageBatchAsync();
                foreach (var message in messages)
                {
                    var isAdded = messageBatch.TryAddMessage(message);
                    if (!isAdded)
                    {
                        await sender.SendMessagesAsync(messageBatch);
                        messageBatch.Dispose();
                        messageBatch = await sender.CreateMessageBatchAsync();
                        messageBatch.TryAddMessage(message);
                    }
                }

                if (messageBatch.Count > 0)
                {
                    await sender.SendMessagesAsync(messageBatch);
                }
            }
            finally
            {
                messageBatch?.Dispose();
            }
        }
    }
}
