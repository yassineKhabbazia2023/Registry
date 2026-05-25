// <copyright file="IAkuiteoContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;
using Application.Requests;

namespace Application.Interfaces;

/// <summary>
/// Exposes the Akuiteo contact-creation workflow.
/// </summary>
public interface IAkuiteoContactService
{
    /// <summary>
    /// Creates a contact in Akuiteo or in mock mode depending on the active configuration.
    /// </summary>
    /// <param name="request">The input payload received by Registry.</param>
    /// <returns>The created Akuiteo contact identifier.</returns>
    Task<AkuiteoContactCreationResponse> CreateContactAsync(AkuiteoContactCreationRequest request);
}
