// <copyright file="MapOffers.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Pulse.Registry.Domain.Entities;

namespace Application.Mappers;

public static class MapOffers
{
    public static IEnumerable<RefOfferEntity> MapOfferCsvsToOfferEntities(this IEnumerable<Offer> source)
    {
        return source?.Select(s => s.MapOfferCsvsToOfferEntities()!).ToList() ?? Enumerable.Empty<RefOfferEntity>();
    }

    public static RefOfferEntity? MapOfferCsvsToOfferEntities(this Offer source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RefOfferEntity()
        {
            AccountNumber = source.AccountNumber,
            ClientEmail = source.ClientEmail,
            CollaboratorEmail = source.CollaboratorEmail,
            MissionLeaderEmail = source.MissionLeaderEmail,
            AccountingExpertEmail = source.AccountingExpertEmail,
            Offer = source.OfferName,
            MigrationStatus = source.MigrationStatus,
        };
    }

    public static Offer MapOfferEntityToOffer(this RefOfferEntity source)
    {
        if (source == null)
        {
            return null;
        }

        return new Offer()
        {
            AccountNumber = source.AccountNumber,
            ClientEmail = source.ClientEmail,
            CollaboratorEmail = source.CollaboratorEmail,
            MissionLeaderEmail = source.MissionLeaderEmail,
            AccountingExpertEmail = source.AccountingExpertEmail,
            OfferName = source.Offer,
            MigrationStatus = source.MigrationStatus,
        };
    }
}
