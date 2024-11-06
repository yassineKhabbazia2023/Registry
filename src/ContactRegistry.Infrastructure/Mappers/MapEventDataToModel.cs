// <copyright file="MapEventDataToModel.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.ContactRegistry.Domain.Constants;

namespace Infrastructure.Mappers;

public static class MapEventDataToModel
{
    public static RoleRegistry RoleEventCreatedDataToModel(this RoleCreatedEventData source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RoleRegistry
        {
            ContactCode = source.ContactId.ToString(),
            ContactEmailOffice = source.ContactEmail,
            AccountNumber = source.AccountNumber,
            RoleFlagStatus = true,
            RoleFunctionDescription = string.Empty,
            RoleSourceName = GlobalConstants.SOURCENAME,
        };
    }

    public static RoleRegistry RoleEventDeletedDataToModel(this RoleDeletedEventData source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RoleRegistry
        {
            ContactCode = source.ContactId.ToString(),
            ContactEmailOffice = source.ContactEmail,
            AccountNumber = source.AccountNumber,
            RoleFlagStatus = false,
            RoleFunctionDescription = string.Empty,
            RoleSourceName = GlobalConstants.SOURCENAME,
        };
    }
}
