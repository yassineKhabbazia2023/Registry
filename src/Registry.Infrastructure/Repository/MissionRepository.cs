// <copyright file="MissionRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using EFCore.BulkExtensions;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

namespace Infrastructure.Repository;

public class MissionRepository(RefContext refContext) : IMissionRepository
{
    public async Task AddMissionsAsync(IEnumerable<RefMissionEntity> missions)
    {
        await refContext.BulkInsertAsync(missions.ToList());
    }
}
