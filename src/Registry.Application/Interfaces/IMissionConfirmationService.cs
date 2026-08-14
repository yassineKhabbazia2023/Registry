// <copyright file="IMissionConfirmationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

/// <summary>
/// Confirms an engagement line once Offer has answered for it.
/// </summary>
public interface IMissionConfirmationService
{
    /// <summary>
    /// Marks the engagement line as SUCCEEDED.
    /// <para>
    /// The acknowledgements carry the engagement code only; the operation comes from the type
    /// of the event received - MissionCreatedEvent for an INSERT, MissionRemovedEvent for a
    /// DELETE. The pair resolves to a single line thanks to
    /// UQ_Missions_Operation_EngagementCode.
    /// </para>
    /// </summary>
    /// <param name="engagementCode">The Akuiteo engagement code carried by the acknowledgement.</param>
    /// <param name="operation">INSERT or DELETE, deduced from the event type.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ConfirmAsync(string engagementCode, string operation, CancellationToken cancellationToken = default);
}
