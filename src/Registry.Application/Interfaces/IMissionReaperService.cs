// <copyright file="IMissionReaperService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

/// <summary>
/// Flips SENT mission lines with no acknowledgement past the configured timeout to FAILED, so
/// the publication pass retries them automatically.
/// </summary>
public interface IMissionReaperService
{
    /// <summary>
    /// Runs one set-based reap pass over mission.Processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ReapUnacknowledgedMissionsAsync(CancellationToken cancellationToken = default);
}
