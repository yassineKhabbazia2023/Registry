// <copyright file="OfferService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Mappers;
using Application.Models;

namespace Application.services
{
    public class OfferService : IOfferService
    {
        private IOfferRepository offerRepository;

        public OfferService(IOfferRepository offerRepository)
        {
            this.offerRepository = offerRepository;
        }

        public async Task SaveOffersAsync(IEnumerable<Offer> offers, Guid batchId)
        {
            await this.offerRepository.SaveOffersAsync(offers.MapOfferCsvsToOfferEntities(), batchId);
        }
    }
}
