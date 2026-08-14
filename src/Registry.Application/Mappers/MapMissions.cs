// <copyright file="MapMissions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Models;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities;
using System.Globalization;

namespace Application.Mappers;

public static class MapMissions
{
    public static IEnumerable<MissionEntity> MapMissionCsvsToMissionEntities(this IEnumerable<MissionCsv> source)
    {
        return source?.Select(s => s.MapMissionCsvToMissionEntity()).ToList() ?? [];
    }

    public static MissionEntity MapMissionCsvToMissionEntity(this MissionCsv source)
    {
        if (source == null)
        {
            return null!;
        }

        return new MissionEntity
        {
            AccountNumber = source.AccountNumber!,
            EngagementCode = source.EngagementCode!,
            OfferCode = source.OfferCode!,
            ProductCode = string.IsNullOrWhiteSpace(source.ProductCode) ? null : source.ProductCode,
            StartDate = DateTime.ParseExact(source.StartDate!, CsvDateFormat.Referential, CultureInfo.InvariantCulture),
            EndDate = DateTime.ParseExact(source.EndDate!, CsvDateFormat.Referential, CultureInfo.InvariantCulture),
            Operation = source.Operation!.ToUpperInvariant(),
            CreatedOn = DateTime.UtcNow,
        };
    }

    public static RegistryMissionCreatedEventData MapMissionToCreatedEventData(this MissionEntity mission)
    {
        ArgumentNullException.ThrowIfNull(mission);

        return new RegistryMissionCreatedEventData
        {
            AccountNumber = mission.AccountNumber,
            EngagementCode = mission.EngagementCode,
            OfferCode = mission.OfferCode,

            // Sent as is, null included: on the Offer side a generic mapping carries a NULL
            // AkuiteoProductCode, which an empty string would never match.
            ProductCode = mission.ProductCode,
            StartDate = mission.StartDate,
            EndDate = mission.EndDate,
        };
    }

    public static RegistryMissionRemovedEventData MapMissionToRemovedEventData(this MissionEntity mission)
    {
        ArgumentNullException.ThrowIfNull(mission);

        return new RegistryMissionRemovedEventData
        {
            EngagementCode = mission.EngagementCode,
        };
    }
}
