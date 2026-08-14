// <copyright file="IMissionProcessingRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Registry.Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Repository interface for mission.Processing operations.
/// Handles reading and updating mission processing status records.
/// </summary>
public interface IMissionProcessingRepository
{
    /// <summary>
    /// Gets the next chunk of mission processing records in the given status, walking
    /// forward with a keyset cursor on RegistryMissionId.
    /// <para>
    /// The cursor is what makes the publication loop terminate. A line whose account is not
    /// known yet stays READY on purpose, so a loop paging with a plain Take() would fetch the
    /// very same chunk for ever. Ordering by RegistryMissionId and asking for what lies
    /// strictly after the last one seen guarantees each pass moves on; the lines left behind
    /// are picked up by the next run, not by the next iteration.
    /// </para>
    /// </summary>
    /// <param name="status">The status to filter by (e.g., READY, SENT, SUCCEEDED).</param>
    /// <param name="afterRegistryMissionId">Exclusive lower bound; pass 0 to start from the beginning.</param>
    /// <param name="chunk">Maximum number of records to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A list of MissionProcessingEntity records ordered by RegistryMissionId.</returns>
    Task<List<MissionProcessingEntity>> GetByStatusAsync(string status, int afterRegistryMissionId, int chunk, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates one or more mission processing records.
    /// </summary>
    /// <param name="entities">The MissionProcessingEntity records to update.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(IEnumerable<MissionProcessingEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the processing record of an engagement line. There is exactly one per line,
    /// the operation being carried by mission.Missions.
    /// </summary>
    /// <param name="registryMissionId">The RegistryMissionId to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The MissionProcessingEntity if found; otherwise null.</returns>
    Task<MissionProcessingEntity?> GetByRegistryMissionIdAsync(int registryMissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new mission processing record.
    /// </summary>
    /// <param name="entity">The MissionProcessingEntity to create.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateAsync(MissionProcessingEntity entity, CancellationToken cancellationToken = default);
}
