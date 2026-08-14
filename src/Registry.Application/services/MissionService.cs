// <copyright file="MissionService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Mappers;
using Application.Models;

namespace Application.Services;

public class MissionService : IMissionService
{
    private readonly IMissionRepository _missionRepository;

    public MissionService(IMissionRepository missionRepository)
    {
        _missionRepository = missionRepository ?? throw new ArgumentNullException(nameof(missionRepository));
    }

    public async Task SaveMissionsAsync(IEnumerable<MissionCsv> missions)
    {
        var missionEntities = missions.MapMissionCsvsToMissionEntities();
        await _missionRepository.AddMissionsAsync(missionEntities);
    }
}
