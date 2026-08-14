// <copyright file="IMissionEventPublisher.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Application.Interfaces;

/// <summary>
/// Publisher of the mission events towards Offer.
/// Mirrors IInvoiceEventPublisher: one batch per operation, on the default registry topic.
/// </summary>
public interface IMissionEventPublisher
{
    /// <summary>
    /// Publishes a batch of RegistryMissionCreatedEvent.
    /// </summary>
    /// <param name="events">Event data list to publish.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BulkPublishAsync(List<RegistryMissionCreatedEventData> events);

    /// <summary>
    /// Publishes a batch of RegistryMissionRemovedEvent.
    /// </summary>
    /// <param name="events">Event data list to publish.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BulkPublishRemovedAsync(List<RegistryMissionRemovedEventData> events);

    /// <summary>
    /// Posts the name of the received csv blob on the mission lines queue, which triggers the
    /// publication without waiting for the scheduled pass.
    /// </summary>
    /// <param name="blobName">Name of the blob holding the received csv.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendMissionLinesBatchEvent(string blobName);
}
