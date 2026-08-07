// <copyright file="MapMissions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Models;
using Pulse.Registry.Domain.Entities;
using System.Globalization;

namespace Application.Mappers;

public static class MapMissions
{
    public static IEnumerable<RefMissionEntity> MapMissionCsvsToMissionEntities(this IEnumerable<RefMissionCsv> source)
    {
        return source?.Select(s => s.MapMissionCsvToMissionEntity()!).ToList() ?? Enumerable.Empty<RefMissionEntity>();
    }

    public static RefMissionEntity? MapMissionCsvToMissionEntity(this RefMissionCsv source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RefMissionEntity
        {
            EntityId = Guid.NewGuid(),
            AccountNumber = source.AccountNumber!,
            EngagementCode = source.EngagementCode!,
            OfferCode = source.OfferCode!,
            ProductCode = string.IsNullOrWhiteSpace(source.ProductCode) ? null : source.ProductCode,
            StartDate = DateTime.ParseExact(source.StartDate!, CsvDateFormat.Referential, CultureInfo.InvariantCulture),
            EndDate = DateTime.ParseExact(source.EndDate!, CsvDateFormat.Referential, CultureInfo.InvariantCulture),
            OperationType = source.Operation!.ToUpperInvariant(),
            OperationDate = DateTime.UtcNow,
        };
    }
}
