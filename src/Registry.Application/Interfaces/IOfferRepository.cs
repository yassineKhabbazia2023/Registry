// <copyright file="IOfferRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Registry.Domain.Entities;

namespace Application.Interfaces
{
    public interface IOfferRepository
    {
        /// <summary>
        /// Saves a collection of offers to the database with the specified batch ID.
        /// </summary>
        /// <param name="offers">The collection of offers to be saved.</param>
        /// <param name="batchId">The unique identifier for the batch associated with the offers.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="offers"/> collection is null.</exception>
        Task SaveOffersAsync(IEnumerable<RefOfferEntity> offers, Guid batchId);
        /// <summary>
        /// Retrieves a collection of offers from the database associated with the specified batch ID.
        /// </summary>
        /// <param name="batchId">The unique identifier for the batch of offers to retrieve.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a collection of <see cref="RefOfferEntity"/> objects
        /// associated with the specified batch ID.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="batchId"/> is an empty GUID.</exception>
        Task<IEnumerable<RefOfferEntity>> GetOffersByBatchIdAsync(Guid batchId);
    }
}
