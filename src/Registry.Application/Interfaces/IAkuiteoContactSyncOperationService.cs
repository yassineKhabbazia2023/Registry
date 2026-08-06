// <copyright file="IAkuiteoContactSyncOperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Back.Events.IntegrationEvents;

namespace Application.Interfaces;

public interface IAkuiteoContactSyncOperationService
{
    /// <summary>
    /// Records a pending Akuiteo contact synchronization operation for a role created from Pulse.
    /// </summary>
    /// <param name="roleEvent">The received role-created event.</param>
    /// <param name="sourceEventId">The source event identifier.</param>
    /// <returns><see langword="true"/> when synchronization tracking is enabled; otherwise, <see langword="false"/>.</returns>
    Task<bool> EnqueueAsync(RoleCreatedEvent roleEvent, Guid sourceEventId);

    /// <summary>
    /// Records an Akuiteo contact synchronization operation using the detected role origin.
    /// </summary>
    /// <param name="roleEvent">The received role-created event.</param>
    /// <param name="sourceEventId">The source event identifier.</param>
    /// <param name="isRoleFromAkuiteo">
    /// <see langword="true"/> when an equivalent INSERT role exists in the Akuiteo reference data.
    /// </param>
    /// <returns><see langword="true"/> when synchronization tracking is enabled; otherwise, <see langword="false"/>.</returns>
    Task<bool> EnqueueAsync(RoleCreatedEvent roleEvent, Guid sourceEventId, bool isRoleFromAkuiteo);

    Task ProcessPendingOperationsAsync();
}
