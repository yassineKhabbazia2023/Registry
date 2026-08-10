// <copyright file="IAkuiteoContactProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Results;

namespace Application.Interfaces;

/// <summary>
/// Sends contact requests to the Akuiteo external API.
/// </summary>
public interface IAkuiteoContactProvider
{
    /// <summary>
    /// Creates a contact in Akuiteo.
    /// </summary>
    /// <param name="request">The outbound Akuiteo payload.</param>
    /// <returns>The provider execution result.</returns>
    Task<AkuiteoContactCreationProviderResult> CreateContactAsync(AkuiteoCreateContactRequest request);

    /// <summary>
    /// Searches Akuiteo contacts using an exact email filter.
    /// </summary>
    /// <param name="email">The contact email address.</param>
    /// <returns>The provider execution result.</returns>
    Task<AkuiteoContactSearchProviderResult> SearchContactsAsync(string email);
}
