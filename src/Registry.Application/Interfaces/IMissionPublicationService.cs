// <copyright file="IMissionPublicationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

/// <summary>
/// Service responsible for publishing pending mission lines to Offer via Service Bus.
/// </summary>
public interface IMissionPublicationService
{
    /// <summary>
    /// Processes pending mission lines from MissionProcessing table (READY status).
    /// Validates accounts in batch, publishes events, and updates processing status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ProcessPendingLinesAsync(CancellationToken cancellationToken = default);
}
