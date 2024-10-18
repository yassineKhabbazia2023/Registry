// <copyright file="MapEventDataToModel.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Infrastructure.Mappers;

public static class MapEventDataToModel
{
    public static RoleRegistry RoleEventDataToModel(this RoleCreatedEventData source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RoleRegistry
        {
            ContactId = source.ContactId,
            AccountId = source.AccountId,
            IsDelegation = source.IsDelegation,
            IsFavorite = source.IsFavorite,
            IsSignatory = source.IsSignatory
        };
    }
}
