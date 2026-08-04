// <copyright file="IInvoiceEventPublisher.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Application.Interfaces;

public interface IInvoiceEventPublisher
{
    /// <summary>
    /// Sends the blob name to the dedicated queue.
    /// </summary>
    /// <param name="blobName">Name of the stored csv blob.</param>
    Task SendInvoiceLinesBatchEvent(string blobName);

    /// <summary>
    /// Publishes one RegistryInvoiceCreatedEvent per event data on the registry topic.
    /// </summary>
    /// <param name="events">Payloads of the created invoices.</param>
    Task SendInvoiceCreatedEventsAsync(List<RegistryInvoiceCreatedEventData> events);

    /// <summary>
    /// Publishes one RegistryInvoiceRemovedEvent per event data on the registry topic.
    /// </summary>
    /// <param name="events">Payloads of the removed invoices.</param>
    Task SendInvoiceRemovedEventsAsync(List<RegistryInvoiceRemovedEventData> events);
}
