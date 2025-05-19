// <copyright file="OfferRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Domain.Entities;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;

namespace Infrastructure.Repository
{
    public class OfferRepository : IOfferRepository
    {
        private RefContext dbContext;

        public OfferRepository(RefContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<RefOfferEntity>> GetOffersByBatchIdAsync(Guid batchId)
        {
            var offers = await this.dbContext.OfferEntities
                .Where(x => x.BatchId == batchId)
                .ToListAsync();

            return offers;
        }

        public async Task SaveOffersAsync(IEnumerable<RefOfferEntity> offers, Guid batchId)
        {

            foreach (var offer in offers)
            {
                offer.BatchId = batchId;
            }

            await this.dbContext.BulkInsertAsync(offers);
        }
    }
}
