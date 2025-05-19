// <copyright file="IOfferEventPublisher.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>


using Application.Models;

namespace Application.Interfaces
{
    public interface IOfferEventPublisher
    {
        /// <summary>
        /// Publishes an event for a batch of offers identified by the given batch ID.
        /// </summary>
        /// <param name="batchId">The unique identifier of the batch to be published.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PublishOfferBatchEventAsync(Guid batchId);

        /// <summary>
        /// Publishes an event for a single offer.
        /// </summary>
        /// <param name="offer">The offer object containing the details to be published.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="offer"/> is null.</exception>
        Task PublishOfferEventAsync(Offer offer);

        /// <summary>
        /// Publishes events for a list of offers in bulk.
        /// </summary>
        /// <param name="offers">The list of offer objects to be published.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="offers"/> list is null or empty.</exception>
        Task BulkPublishOfferEventAsync(List<Application.Models.Offer> offers);

        /// <summary>
        /// Sends an event to notify about a batch of offers identified by the given batch ID.
        /// </summary>
        /// <param name="batchId">The unique identifier of the batch to be sent as an event.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="batchId"/> is an empty GUID.</exception>
        Task SendOfferRegistryBatchEvent(Guid batchId);
    }
}
