// <copyright file="IAkuiteoContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;
using Application.Requests;

namespace Application.Interfaces;

/// <summary>
/// Exposes Akuiteo contact workflows.
/// </summary>
public interface IAkuiteoContactService
{
    /// <summary>
    /// Creates a contact in Akuiteo.
    /// </summary>
    /// <param name="request">The input payload received by Registry.</param>
    /// <returns>The created Akuiteo contact identifier.</returns>
    Task<AkuiteoContactCreationResponse> CreateContactAsync(AkuiteoContactCreationRequest request);

    /// <summary>
    /// Searches contacts in Akuiteo using an exact email filter.
    /// </summary>
    /// <param name="email">The contact email address.</param>
    /// <returns>The matching contacts.</returns>
    Task<IReadOnlyCollection<AkuiteoContactSearchDataResponse>> SearchContactsAsync(string email);
}
