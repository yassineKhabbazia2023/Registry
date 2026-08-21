// <copyright file="MissionRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

namespace Infrastructure.Repository;

public class MissionRepository(RefContext refContext) : IMissionRepository
{
    public async Task AddMissionsAsync(IEnumerable<MissionEntity> missions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(missions);

        var missionList = missions.ToList();
        if (missionList.Count == 0)
        {
            return;
        }

        // SetOutputIdentity fills RegistryMissionId, which is needed to attach each processing
        // row to its mission.
        var bulkConfig = new BulkConfig { SetOutputIdentity = true };
        await refContext.BulkInsertAsync(missionList, bulkConfig, cancellationToken: cancellationToken);

        var processingEntities = missionList.Select(m => new MissionProcessingEntity
        {
            RegistryMissionId = m.RegistryMissionId,
            Status = ProcessStatus.Ready,
            PublishedOn = null,
        }).ToList();

        await refContext.BulkInsertAsync(processingEntities, cancellationToken: cancellationToken);
    }

    public async Task<List<MissionEntity>> GetByRegistryMissionIdsAsync(IEnumerable<int> registryMissionIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registryMissionIds);

        var idList = registryMissionIds.ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        return await refContext.MissionEntity
            .AsNoTracking().Where(m => idList.Contains(m.RegistryMissionId)).ToListAsync(cancellationToken);
    }

    public async Task<MissionEntity?> GetByEngagementCodeAndOperationAsync(string engagementCode, string operation, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engagementCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        // UQ_Missions_Operation_EngagementCode guarantees at most one row: this is the key Offer
        // confirms on, the operation being deduced from the type of the event received.
        return await refContext.MissionEntity
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Operation == operation && m.EngagementCode == engagementCode, cancellationToken);
    }

    public async Task<List<(string Operation, string EngagementCode)>> GetExistingEngagementKeysAsync(IEnumerable<string> engagementCodes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(engagementCodes);

        var codes = engagementCodes.Distinct().ToList();
        if (codes.Count == 0)
        {
            return [];
        }

        var rows = await refContext.MissionEntity
            .AsNoTracking()
            .Where(m => codes.Contains(m.EngagementCode))
            .Select(m => new { m.Operation, m.EngagementCode })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Operation, r.EngagementCode)).ToList();
    }
}
