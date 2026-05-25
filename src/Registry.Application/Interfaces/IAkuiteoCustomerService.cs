// <copyright file="IAkuiteoCustomerService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;
using Application.Requests;

namespace Application.Interfaces;

/// <summary>
/// Exposes the Akuiteo customer-creation workflow.
/// </summary>
public interface IAkuiteoCustomerService
{
    /// <summary>
    /// Creates a customer in Akuiteo or in mock mode depending on the active configuration.
    /// </summary>
    /// <param name="request">The input payload received by Registry.</param>
    /// <returns>The created Akuiteo account number.</returns>
    Task<AkuiteoCustomerCreationResponse> CreateCustomerAsync(AkuiteoCustomerCreationRequest request);
}
