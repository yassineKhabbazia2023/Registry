// <copyright file="MissionProcessingRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

namespace Infrastructure.Repository;

public class MissionProcessingRepository(RefContext refContext) : IMissionProcessingRepository
{
    public async Task<List<MissionProcessingEntity>> GetByStatusAsync(string status, int afterRegistryMissionId, int chunk, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        if (chunk <= 0)
        {
            throw new ArgumentException("Chunk size must be greater than 0", nameof(chunk));
        }

        return await refContext.MissionProcessingEntity
            .AsNoTracking()
            .Where(m => m.Status == status && m.RegistryMissionId > afterRegistryMissionId)
            .OrderBy(m => m.RegistryMissionId)
            .Take(chunk)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(IEnumerable<MissionProcessingEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return;
        }

        foreach (var entity in entityList)
        {
            refContext.MissionProcessingEntity.Update(entity);
        }

        await refContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MissionProcessingEntity?> GetByRegistryMissionIdAsync(int registryMissionId, CancellationToken cancellationToken = default)
    {
        return await refContext.MissionProcessingEntity
            .AsNoTracking().FirstOrDefaultAsync(m => m.RegistryMissionId == registryMissionId, cancellationToken);
    }

    public async Task CreateAsync(MissionProcessingEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        refContext.MissionProcessingEntity.Add(entity);
        await refContext.SaveChangesAsync(cancellationToken);
    }
}
