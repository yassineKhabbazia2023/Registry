// <copyright file="OfferEventPublisher.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Options;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Registry.Infrastructure.Managers;

namespace Pulse.Offer.Infrastructure.Providers;

public class OfferEventPublisher : IOfferEventPublisher
{
    private INotificationManager notificationManager;
    private IServiceBusMessageFactory serviceBusMessageFactory;
    private OffersMigrationOptions options;
    private IAzureClientFactory<ServiceBusClient> clientFactory;
    private ServiceBusClient serviceBusClient;

    public OfferEventPublisher(INotificationManager notificationManager, IServiceBusMessageFactory serviceBusMessageFactory, IOptions<OffersMigrationOptions> options)
    {
        this.notificationManager = notificationManager;
        this.serviceBusMessageFactory = serviceBusMessageFactory;
        this.options = options.Value;
    }

    public async Task PublishOfferBatchEventAsync(Guid batchId)
    {
        var eventData = new RegistryOfferBatchEventData
        {
            BatchId = batchId,
        };


        var @event = new RegistryOfferBatchEvent(eventData);
        await this.notificationManager.PublishAsync(@event);
    }

    public async Task SendOfferRegistryBatchEvent(Guid batchId)
    {
        await this.notificationManager.SendMessageToQueueAsync(batchId.ToString(), this.options.RegistryOfferBatchQueueName);
    }

    public async Task PublishOfferEventAsync(Application.Models.Offer offer)
    {
        var eventData = new RegistryOfferUpdatedEventData
        {
            Offer = offer.OfferName,
            AccountNumber = offer.AccountNumber,
            ClientEmail = offer.ClientEmail!,
            CollaboratorEmail = offer.CollaboratorEmail!,
            MissionLeaderEmail = offer.MissionLeaderEmail!,
            AccountingExpertEmail = offer.AccountingExpertEmail!,

        };

        var @event = new RegistryOfferUpdatedEvent(eventData);

        await this.notificationManager.PublishAsync(@event);

    }

    public async Task BulkPublishOfferEventAsync(List<Application.Models.Offer> offers)
    {
        List<ServiceBusMessage> messagesToSend = [];
        foreach (var offer in offers)
        {
            var eventData = new RegistryOfferUpdatedEventData
            {
                Offer = offer.OfferName,
                AccountNumber = offer.AccountNumber,
                ClientEmail = offer.ClientEmail!,
                CollaboratorEmail = offer.CollaboratorEmail!,
                MissionLeaderEmail = offer.MissionLeaderEmail!,
                AccountingExpertEmail = offer.AccountingExpertEmail!,
                MigrationStatus = offer.MigrationStatus,
            };
            var @event = new RegistryOfferUpdatedEvent(eventData);
            var message = this.serviceBusMessageFactory.CreateMessage(@event);
            messagesToSend.Add(message);
        }

        await this.notificationManager.BulkPublishAsync(messagesToSend);
    }
}
