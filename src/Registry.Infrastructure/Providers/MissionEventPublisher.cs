// <copyright file="MissionEventPublisher.cs" company="Pulse">
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

/// <summary>
/// Publishes the mission events towards Offer, on the default registry topic.
/// <para>
/// Goes through INotificationManager and IServiceBusMessageFactory like every other
/// publisher of the repository (see InvoiceEventPublisher). This is not a style choice:
/// the factory wraps the payload in its BaseEvent envelope and promotes EventType into the
/// message application properties, which is exactly what the subscription filters of
/// servicebus.yaml read - "EventType = 'RegistryMissionCreatedEvent'". A hand-built
/// ServiceBusMessage carrying the bare payload matches no subscription at all.
/// </para>
/// </summary>
public class MissionEventPublisher : IMissionEventPublisher
{
    private readonly INotificationManager notificationManager;
    private readonly IServiceBusMessageFactory serviceBusMessageFactory;
    private readonly MissionOptions options;

    public MissionEventPublisher(INotificationManager notificationManager, IServiceBusMessageFactory serviceBusMessageFactory, IOptions<MissionOptions> options)
    {
        this.notificationManager = notificationManager ?? throw new ArgumentNullException(nameof(notificationManager));
        this.serviceBusMessageFactory = serviceBusMessageFactory ?? throw new ArgumentNullException(nameof(serviceBusMessageFactory));
        this.options = options.Value;
    }

    /// <inheritdoc/>
    public async Task SendMissionLinesBatchEvent(string blobName)
    {
        if (string.IsNullOrWhiteSpace(this.options.MissionLinesQueueName))
        {
            throw new ServiceBusOperationException("Mission lines queue name is not configured.");
        }

        await this.notificationManager.SendMessageToQueueAsync(blobName, this.options.MissionLinesQueueName);
    }

    /// <inheritdoc/>
    public Task BulkPublishAsync(List<RegistryMissionCreatedEventData> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return this.BulkPublishEventsAsync(events.Select(data => new RegistryMissionCreatedEvent(data)));
    }

    /// <inheritdoc/>
    public Task BulkPublishRemovedAsync(List<RegistryMissionRemovedEventData> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return this.BulkPublishEventsAsync(events.Select(data => new RegistryMissionRemovedEvent(data)));
    }

    private async Task BulkPublishEventsAsync<TEventData>(IEnumerable<BaseEvent<TEventData>> events)
    {
        var messages = events
            .Select(e => this.serviceBusMessageFactory.CreateMessage(e))
            .ToList();

        if (messages.Count == 0)
        {
            return;
        }

        await this.notificationManager.BulkPublishAsync(messages);
    }
}
