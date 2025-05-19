// <copyright file="IOfferService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces
{
    public interface IOfferService
    {
        /// <summary>
        /// Saves a collection of offers to the database with the specified batch ID.
        /// </summary>
        /// <param name="offern">The collection of offers to be saved.</param>
        /// <param name="batchId">The unique identifier for the batch associated with the offers.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="offern"/> collection is null.</exception>
        Task SaveOffersAsync(IEnumerable<Offer> offern, Guid batchId);
    }
}
