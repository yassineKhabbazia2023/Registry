// <copyright file="MapContacts.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Pulse.ContactRegistry.Domain.Entities;

namespace Infrastructure.Mappers;

public static class MapContacts
{
    public static IEnumerable<RefContactEntity> MapContactCsvsToContactEntities(this IEnumerable<RefContactCsv> source)
    {
        return source?.Select(s => s.MapContactCsvToContactEntity()!).ToList() ?? Enumerable.Empty<RefContactEntity>();
    }

    public static RefContactEntity? MapContactCsvToContactEntity(this RefContactCsv source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RefContactEntity
        {
            ContactFlagStatus = source.ContactFlagStatus,
            Email = source.Email,
            FirstName = source.FirstName,
            LastName = source.LastName,
            IsCustomer = source.IsCustomer,
            LandPhone = source.LandPhone,
            MobilePhone = source.MobilePhone,
            JobDescription = source.JobDescription,
            OfficeCode = source.OfficeCode,
            OperationType = source.Operation,
            OperationDate = DateTime.UtcNow,
        };
    }
}
