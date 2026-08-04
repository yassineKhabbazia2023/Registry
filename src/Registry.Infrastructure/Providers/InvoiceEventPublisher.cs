// <copyright file="InvoiceEventPublisher.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Options;
using Microsoft.Extensions.Options;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Registry.Infrastructure.Managers;

namespace Infrastructure.Providers;

public class InvoiceEventPublisher : IInvoiceEventPublisher
{
    private readonly INotificationManager notificationManager;
    private readonly IServiceBusMessageFactory serviceBusMessageFactory;
    private readonly InvoiceOptions options;

    public InvoiceEventPublisher(INotificationManager notificationManager, IServiceBusMessageFactory serviceBusMessageFactory, IOptions<InvoiceOptions> options)
    {
        this.notificationManager = notificationManager;
        this.serviceBusMessageFactory = serviceBusMessageFactory;
        this.options = options.Value;
    }

    public async Task SendInvoiceLinesBatchEvent(string blobName)
    {
        if (string.IsNullOrWhiteSpace(this.options.InvoiceLinesQueueName))
        {
            throw new ServiceBusOperationException("Invoice lines queue name is not configured.");
        }

        await this.notificationManager.SendMessageToQueueAsync(blobName, this.options.InvoiceLinesQueueName);
    }

    public Task SendInvoiceCreatedEventsAsync(List<RegistryInvoiceCreatedEventData> events)
    {
        return this.BulkPublishEventsAsync(events.Select(data => new RegistryInvoiceCreatedEvent(data)));
    }

    public Task SendInvoiceRemovedEventsAsync(List<RegistryInvoiceRemovedEventData> events)
    {
        return this.BulkPublishEventsAsync(events.Select(data => new RegistryInvoiceRemovedEvent(data)));
    }

    private async Task BulkPublishEventsAsync<TEventData>(IEnumerable<BaseEvent<TEventData>> events)
    {
        var messages = events
            .Select(e => this.serviceBusMessageFactory.CreateMessage(e))
            .ToList();

        await this.notificationManager.BulkPublishAsync(messages);
    }
}
