// <copyright file="IContactAkuiteoSynchronizer.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Back.Events.IntegrationEvents.EventsData;
using Application.Models.Results;

namespace Application.Interfaces;

/// <summary>
/// Synchronizes a Pulse contact with Akuiteo after the contact receives an account role.
/// </summary>
public interface IContactAkuiteoSynchronizer
{
    /// <summary>
    /// Creates or attaches the contact in Akuiteo for the account carried by the role event.
    /// </summary>
    /// <param name="role">The created role event data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<ContactAkuiteoSynchronizationResult> SynchronizeAsync(
        RoleCreatedEventData role,
        CancellationToken cancellationToken = default);
}
