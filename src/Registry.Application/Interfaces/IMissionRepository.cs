// <copyright file="IMissionRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Registry.Domain.Entities;

namespace Application.Interfaces;

public interface IMissionRepository
{
    /// <summary>
    /// Persists new missions (mission.Missions) and creates their corresponding
    /// MissionProcessing rows (status READY) in a single unit of work.
    /// </summary>
    /// <param name="missions">The missions to insert, each carrying its own operation (INSERT/DELETE).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddMissionsAsync(IEnumerable<MissionEntity> missions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets mission.Missions records by their RegistryMissionId.
    /// </summary>
    /// <param name="registryMissionIds">The list of RegistryMissionIds to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A list of MissionEntity records.</returns>
    Task<List<MissionEntity>> GetByRegistryMissionIdsAsync(IEnumerable<int> registryMissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the single engagement line matching an engagement code and an operation.
    /// This is the correlation key of the acknowledgements coming back from Offer: the payload
    /// carries the engagement code, the operation is deduced from the event type received
    /// (MissionCreatedEvent =&gt; INSERT, MissionRemovedEvent =&gt; DELETE).
    /// </summary>
    /// <param name="engagementCode">The Akuiteo engagement code.</param>
    /// <param name="operation">INSERT or DELETE.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The MissionEntity if found; otherwise null.</returns>
    Task<MissionEntity?> GetByEngagementCodeAndOperationAsync(string engagementCode, string operation, CancellationToken cancellationToken = default);
}
