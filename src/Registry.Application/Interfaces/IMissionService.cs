// <copyright file="IMissionService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IMissionService
{
    Task SaveMissionsAsync(IEnumerable<MissionCsv> missions);

    /// <summary>
    /// Gets the (Operation, EngagementCode) pairs already persisted among the given csv lines.
    /// </summary>
    /// <param name="missions">The csv lines to look up.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The existing (Operation, EngagementCode) pairs.</returns>
    Task<List<(string Operation, string EngagementCode)>> GetExistingEngagementKeysAsync(IEnumerable<MissionCsv> missions, CancellationToken cancellationToken = default);
}

