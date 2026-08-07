// <copyright file="MissionService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Mappers;
using Application.Models;

namespace Application.Services;

public class MissionService : IMissionService
{
    private readonly IMissionRepository missionRepository;

    public MissionService(IMissionRepository missionRepository)
    {
        this.missionRepository = missionRepository;
    }

    public async Task SaveMissionsAsync(IEnumerable<RefMissionCsv> missions)
    {
        var missionEntities = missions.MapMissionCsvsToMissionEntities();
        await missionRepository.AddMissionsAsync(missionEntities);
    }
}
