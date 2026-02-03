// <copyright file="MapContacts.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Pulse.Registry.Domain.Entities;

namespace Application.Mappers;

public static class MapRoles
{
    public static IEnumerable<RefRoleEntity> MapRoleCsvsToRoleEntities(this IEnumerable<RefRoleCsv> source)
    {
        return source?.Select(s => s.MapRoleCsvToRoleEntity()!).ToList() ?? Enumerable.Empty<RefRoleEntity>();
    }

    public static RefRoleEntity? MapRoleCsvToRoleEntity(this RefRoleCsv source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RefRoleEntity
        {
            RoleFlagStatus = source.RoleFlagStatus,
            ContactEmail = source.ContactEmail,
            AccountNumber = source.AccountNumber,
            Description = source.Description,
            ContactFlagPortailFactures = source.ContactFlagPortailFactures,
            OperationType = source.Operation,
            OperationDate = DateTime.UtcNow,
        };
    }
}
