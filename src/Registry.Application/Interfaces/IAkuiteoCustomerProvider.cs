// <copyright file="IAkuiteoCustomerProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Results;

namespace Application.Interfaces;

/// <summary>
/// Sends customer-creation requests to the Akuiteo external API.
/// </summary>
public interface IAkuiteoCustomerProvider
{
    /// <summary>
    /// Creates a customer in Akuiteo.
    /// </summary>
    /// <param name="request">The outbound Akuiteo payload.</param>
    /// <returns>The provider execution result.</returns>
    Task<AkuiteoCustomerCreationProviderResult> CreateCustomerAsync(AkuiteoCreateCustomerRequest request);
}
