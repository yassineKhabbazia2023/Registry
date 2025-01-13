// <copyright file="MapProcessDeltaTrigger.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Pulse.ContactRegistry.Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class MapProcessDeltaTrigger
{
    public static RegProcessDeltaTrigger? MapProcessDeltaTriggerEntityToModel(this RegProcessDeltaTriggerEntity source)
    {
        if (source == null)
        {
            return null!;
        }

        return new RegProcessDeltaTrigger
        {
            Id = source.Id,
            Account = source.Account,
            Contact = source.Contact,
            Role = source.Role
        };
    }
}
